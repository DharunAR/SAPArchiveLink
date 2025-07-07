using System.Globalization;
using System.Text;

namespace SAPArchiveLink
{
    public class ContentServerRequestAuthenticator
    {
        private readonly IVerifier _verifier;
        private readonly ILogger<ContentServerRequestAuthenticator> _logger;
        private readonly ICommandResponseFactory _responseFactory;

        private const string DefaultCharset = "UTF-8";
        private const string ExpirationFormat = "yyyyMMddHHmmss";

        private readonly HashSet<ALCommandTemplate> UnsupportedCommands = new()
        {
            ALCommandTemplate.ADMINCONTREP,
            ALCommandTemplate.APPENDNOTE,
            ALCommandTemplate.GETANNOTATIONS,
            ALCommandTemplate.GETNOTES,
            ALCommandTemplate.STOREANNOTATIONS
        };

        public ContentServerRequestAuthenticator(IVerifier verifier, ILogger<ContentServerRequestAuthenticator> logger,
                                                    ICommandResponseFactory responseFactory)
        {
            _verifier = verifier;
            _logger = logger;
            _responseFactory = responseFactory;
        }

        public RequestAuthResult CheckRequest(CommandRequest request, ICommand command, IArchiveCertificate certificates)
        {
            var pVersion = command.GetValue(ALParameter.VarPVersion);
            _logger.LogDebug($"Validating command {command.GetTemplate()} with version {pVersion}");

            if (command.GetTemplate() != ALCommandTemplate.ADMINCONTREP && !IsSupportedVersion(pVersion))
                return Fail("Unsupported protocol version", StatusCodes.Status400BadRequest);

            if (UnsupportedCommands.Contains(command.GetTemplate()))
                return Fail($"Command {command.GetTemplate()} is not supported", StatusCodes.Status501NotImplemented);

            if (command.GetTemplate() == ALCommandTemplate.SIGNURL && !request.HttpRequest.IsHttps)
                return Fail("SIGNURL requires HTTPS", StatusCodes.Status400BadRequest);

            if (!ValidateContentHeadersIfNeeded(command, request.HttpRequest))
                return Fail("Invalid or missing Content headers", StatusCodes.Status400BadRequest);

            return CheckAuthentication(command, certificates);
        }

        private RequestAuthResult CheckAuthentication(ICommand command, IArchiveCertificate certificates)
        {
            bool requiresSignature = !string.IsNullOrEmpty(command.GetValue(ALParameter.VarSecKey)) ||
            !string.IsNullOrEmpty(command.GetValue(ALParameter.VarRmsPi)) ||
            !string.IsNullOrEmpty(command.GetValue(ALParameter.VarRmsNode)) ||
            command.GetTemplate() == ALCommandTemplate.SIGNURL;

            if (!requiresSignature)
                return RequestAuthResult.Success();

            try
            {
                VerifyUrl(command, certificates);

                if (!string.IsNullOrEmpty(command.GetValue(ALParameter.VarRmsPi)) && !command.IsImmutable())
                    return Fail($"{ALParameter.VarRmsPi} is set but command is not immutable", StatusCodes.Status403Forbidden);

                return RequestAuthResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication verification failed");
                return Fail("Authentication verification error: " + ex.Message, StatusCodes.Status403Forbidden);
            }
        }

        private void VerifyUrl(ICommand command, IArchiveCertificate certificates)
        {
            _verifier.SetCertificates(certificates);

            var charset = command.GetURLCharset() ?? DefaultCharset;
            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(charset);
                var secKey = command.GetValue(ALParameter.VarSecKey) ?? throw new InvalidOperationException("Missing secKey");

                var authId = command.GetValue(ALParameter.VarAuthId);
                var expiration = command.GetValue(ALParameter.VarExpiration);
                var accessModeStr = command.GetValue(ALParameter.VarAccessMode);

                if (string.IsNullOrEmpty(authId) || string.IsNullOrEmpty(expiration) || string.IsNullOrEmpty(accessModeStr))
                    throw new InvalidOperationException("Missing authId, expiration, or accessMode");

                CheckExpiration(expiration);

                if (!accessModeStr.Contains(command.GetAccessMode().ToString()))
                    throw new UnauthorizedAccessException($"Access mode {command.GetAccessMode()} not permitted");

                _verifier.SetSignedData(Convert.FromBase64String(secKey));
                _verifier.SetRequiredPermission(SecurityUtils.AccessModeToInt(command.GetAccessMode()));

                string stringToSign = command.GetStringToSign(false, charset);
                _verifier.VerifyAgainst(encoding.GetBytes(stringToSign));

                var cert = _verifier.GetCertificate()
                ?? throw new UnauthorizedAccessException("No valid certificate found");

                command.SetVerified();
                command.SetCertSubject(cert.Subject);
                command.SetImmutable();

                _logger.LogInformation($"Request verified. Subject: {cert.Subject}");
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException($"Unsupported charset: {charset}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification failed");
                throw new UnauthorizedAccessException("Verification failed: " + ex.Message);
            }
        }

        private bool ValidateContentHeadersIfNeeded(ICommand command, HttpRequest request)
        {
            if (command.IsHttpPOST() || command.IsHttpPUT())
            {
                if (request.ContentLength is null or < 0)
                {
                    string err = "Content-Length header is missing or invalid.";
                    _logger.LogError(err);
                    return false;
                }
            }
            return true;
        }

        private void CheckExpiration(string expiration)
        {
            if (DateTime.TryParseExact(expiration, ExpirationFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exp))
            {
                if (exp < DateTime.UtcNow)
                    throw new UnauthorizedAccessException("Request has expired");
            }
            else
            {
                throw new FormatException("Invalid expiration format");
            }
        }

        private bool IsSupportedVersion(string pVersion) =>
        !string.IsNullOrWhiteSpace(pVersion) && IsSupported(ParseVersion(pVersion));

        private ALProtocolVersion ParseVersion(string version) => version switch
        {
            "0045" => ALProtocolVersion.OO45,
            "0046" => ALProtocolVersion.OO46,
            "0047" => ALProtocolVersion.OO47,
            _ => ALProtocolVersion.Unsupported
        };

        private bool IsSupported(ALProtocolVersion version) =>
        version is ALProtocolVersion.OO45 or ALProtocolVersion.OO46 or ALProtocolVersion.OO47;

        private RequestAuthResult Fail(string message, int statusCode)
        {
            var error = _responseFactory.CreateError(message, statusCode);
            return RequestAuthResult.Fail(error);
        }
    }

}
