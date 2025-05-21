using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SAPArchiveLink
{
    public class ContentServerRequestAuthenticator
    {
        private readonly IVerifier _verifier;
        private readonly ILogger<ContentServerRequestAuthenticator> _logger;

        public ContentServerRequestAuthenticator(IVerifier verifier, ILogger<ContentServerRequestAuthenticator> logger)
        {
            _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
            _logger = logger;
        }

        public X509Certificate2 CheckRequest(CommandRequest request, ICommand command, List<IArchiveCertificate> certificates)
        {
            _logger.LogDebug("Validating command {CommandName} with version {Version}", command.GetTemplate(), command.GetValue("pVersion"));

            string pVersion = command.GetValue("pVersion");
            if (command.GetTemplate() != ALCommandTemplate.ADMINCONTREP && !IsSupportedVersion(pVersion))
            {
                throw new ALException(ALException.AL_VERSION_NOT_SUPPORTED, ALException.AL_VERSION_NOT_SUPPORTED_STR,
                    new object[] { pVersion, "0045, 0046, 0047" }, StatusCodes.Status400BadRequest);
            }

            if (command.GetTemplate() is ALCommandTemplate.ADMINCONTREP or ALCommandTemplate.APPENDNOTE
                or ALCommandTemplate.GETANNOTATIONS or ALCommandTemplate.GETNOTES or ALCommandTemplate.STOREANNOTATIONS)
            {
                throw new ALException(ALException.AL_METHOD_NOT_SUPPORTED, ALException.AL_METHOD_NOT_SUPPORTED_STR,
                    new object[] { command.GetTemplate() }, StatusCodes.Status501NotImplemented);
            }

            if (command.GetTemplate() == ALCommandTemplate.SIGNURL && !request.HttpRequest.IsHttps)
            {
                throw new ALException(ALException.AL_NON_SSL_ACCESS, ALException.AL_NON_SSL_ACCESS_STR,
                    new object[] { command.GetValue("contRep") ?? "unknown" }, StatusCodes.Status403Forbidden);
            }

            ValidateContentHeadersIfNeeded(command, request.HttpRequest);
            ValidateChecksumIfPresent(request);
            ValidateOriginalLength(request, command);

            return CheckAuthentication(command, certificates);
        }

        private X509Certificate2 CheckAuthentication(ICommand command, List<IArchiveCertificate> certificates)
        {
            bool requiresSignature = !string.IsNullOrEmpty(command.GetValue("secKey")) ||
                                     !string.IsNullOrEmpty(command.GetValue("rmspi")) ||
                                     !string.IsNullOrEmpty(command.GetValue("rmsnode")) ||
                                     command.GetTemplate() == ALCommandTemplate.SIGNURL;

            if (!requiresSignature)
                return null;

            try
            {
                var cert = VerifyUrl(command, certificates);

                if (command.GetValue("rmspi") is not null or "" && !command.IsImmutable())
                {
                    throw new ALException(ALException.AL_UNEXPECTED_PARAMETER, ALException.AL_UNEXPECTED_PARAMETER_STR,
                        new object[] { "rmspi|rmsnode", command.GetTemplate() }, StatusCodes.Status400BadRequest);
                }

                return cert;
            }
            catch (Exception ex)
            {
                throw new ALException(ALException.AL_SIGNED_URL_ERROR, ALException.AL_SIGNED_URL_ERROR_STR,
                    new object[] { command.GetTemplate() }, StatusCodes.Status403Forbidden, ex);
            }
        }

        private X509Certificate2 VerifyUrl(ICommand command, List<IArchiveCertificate> certificates)
        {
            _verifier.SetCertificates(certificates);

            string charset = command.GetURLCharset() ?? "UTF-8";
            string secKey = command.GetValue("secKey");
            if (string.IsNullOrEmpty(secKey))
                throw new ALException("MISSING_SIGNATURE", "Missing secKey parameter", null, StatusCodes.Status403Forbidden);

            string authId = command.GetValue("authId");
            string expiration = command.GetValue("expiration");
            string accessModeStr = command.GetValue("accessMode");

            if (string.IsNullOrEmpty(authId) || string.IsNullOrEmpty(expiration) || string.IsNullOrEmpty(accessModeStr))
            {
                throw new ALException(ALException.AL_SIGNED_URL_ERROR, ALException.AL_SIGNED_URL_ERROR_STR,
                    new object[] { "Missing required signed URL fields" }, StatusCodes.Status403Forbidden);
            }

            CheckExpiration(expiration);

            if (!accessModeStr.Contains(command.GetAccessMode().ToString()))
            {
                throw new ALException("INVALID_ACCESSMODE", "Access mode not allowed for signed URL",
                    new object[] { accessModeStr, command.GetAccessMode().ToString(), command.GetTemplate() }, StatusCodes.Status403Forbidden);
            }

            _verifier.SetSignedData(Convert.FromBase64String(secKey));
            _verifier.SetRequiredPermission(SecurityUtils.AccessModeToInt(command.GetAccessMode()));

            if (command is { })
            {
                string stringToSign = command.GetStringToSign(false, charset);
                _verifier.VerifyAgainst(Encoding.GetEncoding(charset).GetBytes(stringToSign));
            }

            var cert = _verifier.GetCertificate() ?? throw new ALException("CERTIFICATE_MISSING", "No certificate found after verification", null, StatusCodes.Status403Forbidden);

            command.SetVerified();
            command.SetCertSubject(cert.Subject);
            command.SetImmutable();

            _logger.LogInformation("Verified request signed by: {Subject}", cert.Subject);
            return cert;
        }

        private static void ValidateContentHeadersIfNeeded(ICommand command, HttpRequest request)
        {
            if (command.IsHttpPOST() || command.IsHttpPUT())
            {
                if (request.ContentLength is null or < 0)
                    throw new ALException(ALException.AL_ERROR_MISSING_ATTRIBUTE, ALException.AL_ERROR_MISSING_ATTRIBUTE_STR,
                        new object[] { "content-length" }, StatusCodes.Status400BadRequest);

                if (string.IsNullOrEmpty(request.ContentType))
                    throw new ALException(ALException.AL_ERROR_MISSING_ATTRIBUTE, ALException.AL_ERROR_MISSING_ATTRIBUTE_STR,
                        new object[] { "content-type" }, StatusCodes.Status400BadRequest);
            }
        }

        private void ValidateChecksumIfPresent(CommandRequest request)
        {
            var checksum = request.HttpRequest.Query["ixCheckSum"].FirstOrDefault()
                           ?? request.HttpRequest.Headers["x-ix-checksum"].FirstOrDefault();

            if (!string.IsNullOrEmpty(checksum))
            {
                var query = request.HttpRequest.QueryString.Value;
                var computed = ComputeChecksum(query);

                if (!checksum.Equals(computed, StringComparison.OrdinalIgnoreCase))
                    throw new ALException("INVALID_CHECKSUM", "Checksum mismatch", new object[] { computed, checksum }, StatusCodes.Status400BadRequest);
            }
        }

        private void ValidateOriginalLength(CommandRequest request, ICommand command)
        {
            if (request.HttpRequest.Headers.TryGetValue("x-original-length", out var value) &&
                long.TryParse(value, out long originalLength) &&
                (command.IsHttpPOST() || command.IsHttpPUT()))
            {
                var actualLength = request.HttpRequest.ContentLength ?? -1;
                if (originalLength != actualLength)
                {
                    throw new ALException("ORIGINAL_LENGTH_MISMATCH", "Expected length {0}, got {1}",
                        new object[] { originalLength, actualLength }, StatusCodes.Status400BadRequest);
                }
            }
        }

        private void CheckExpiration(string expiration)
        {
            if (DateTime.TryParseExact(expiration, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exp) &&
                exp < DateTime.UtcNow)
            {
                throw new ALException("EXPIRED_SIGNATURE", "The signature has expired at {0}", new object[] { exp }, StatusCodes.Status403Forbidden);
            }
        }

        private string ComputeChecksum(string data)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(data ?? string.Empty);
            return Convert.ToBase64String(sha256.ComputeHash(bytes));
        }

        private bool IsSupportedVersion(string pVersion)
        {
            if (!string.IsNullOrWhiteSpace(pVersion))
            {
                return IsSupported(pVersionParse(pVersion));
            }
            return false;
        }

        private ALProtocolVersion pVersionParse(string version)
        {
            return version switch
            {
                "0045" => ALProtocolVersion.OO45,
                "0046" => ALProtocolVersion.OO46,
                "0047" => ALProtocolVersion.OO47,
                _ => ALProtocolVersion.Unsupported
            };
        }

        private bool IsSupported(ALProtocolVersion version)
        {
            return version is ALProtocolVersion.OO45 or ALProtocolVersion.OO46 or ALProtocolVersion.OO47;
        }
    }

}
