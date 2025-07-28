using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SAPArchiveLink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ContentServerRequestAuthenticatorTests
    {
        private Mock<IVerifier> _verifierMock;
        private Mock<ILogHelper<ContentServerRequestAuthenticator>> _loggerMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private ContentServerRequestAuthenticator _authenticator;
        private Mock<ICommand> _commandMock;
        private Mock<IArchiveCertificate> _certMock;
        private Mock<ICommandResponse> _errorResponseMock;
        private CommandRequest _request;

        [SetUp]
        public void SetUp()
        {
            _verifierMock = new Mock<IVerifier>();
            _loggerMock = new Mock<ILogHelper<ContentServerRequestAuthenticator>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _commandMock = new Mock<ICommand>();
            _certMock = new Mock<IArchiveCertificate>();
            _errorResponseMock = new Mock<ICommandResponse>();

            _authenticator = new ContentServerRequestAuthenticator(
                _verifierMock.Object,
                _loggerMock.Object,
                _responseFactoryMock.Object
            );

            _request = new CommandRequest
            {
                Url = "https://localhost/test",
                HttpMethod = "GET",
                Charset = "UTF-8",
                HttpRequest = new DefaultHttpContext().Request
            };
        }

        [Test]
        public void CheckRequest_UnsupportedProtocolVersion_ReturnsFail()
        {
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.GET);
            _commandMock.Setup(c => c.IsHttpGET()).Returns(true);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("9999");
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_errorResponseMock.Object);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.EqualTo(_errorResponseMock.Object));
        }

        [Test]
        public void CheckRequest_UnsupportedCommand_ReturnsFail()
        {
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.ADMINCONTREP);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0045");
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_errorResponseMock.Object);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.EqualTo(_errorResponseMock.Object));

        }

        [Test]
        public void CheckRequest_SignUrlWithoutHttps_ReturnsFail()
        {
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.SIGNURL);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0045");
            var httpRequest = new DefaultHttpContext().Request;
            httpRequest.Scheme = "http";
            _request.HttpRequest = httpRequest;
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_errorResponseMock.Object);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.EqualTo(_errorResponseMock.Object));
        }

        [Test]
        public void CheckRequest_MissingContentLengthHeader_ReturnsFail()
        {
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.CREATEPOST);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0045");
            _commandMock.Setup(c => c.IsHttpPOST()).Returns(true);
            _request.HttpRequest.ContentLength = null;
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_errorResponseMock.Object);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.EqualTo(_errorResponseMock.Object));
        }

        [Test]
        public void CheckRequest_ValidRequest_NoSignature_ReturnsSuccess()
        {
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.GET);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0045");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns((string)null);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.Null);
            //Assert.That(GetFailResponse(result), Is.Null);
        }

        [Test]
        public void CheckRequest_ValidRequest_WithSignature_ReturnsSuccess()
        {
            byte[] dummyCertBytes = Convert.FromBase64String("MIICsDCCAhmgAwIBAgIJALwzrJEIBOaeMA0GCSqGSIb3DQEBBQUAMEUxCzAJBgNV\r\nBAYTAkFVMRMwEQYDVQQIEwpTb21lLVN0YXRlMSEwHwYDVQQKExhJbnRlcm5ldCBX\r\naWRnaXRzIFB0eSBMdGQwHhcNMTEwOTMwMTUyNjM2WhcNMjEwOTI3MTUyNjM2WjBF\r\nMQswCQYDVQQGEwJBVTETMBEGA1UECBMKU29tZS1TdGF0ZTEhMB8GA1UEChMYSW50\r\nZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKB\r\ngQC88Ckwru9VR2p2KJ1WQyqesLzr95taNbhkYfsd0j8Tl0MGY5h+dczCaMQz0YY3\r\nxHXuU5yAQQTZjiks+D3KA3cx+iKDf2p1q77oXxQcx5CkrXBWTaX2oqVtHm3aX23B\r\nAIORGuPk00b4rT3cld7VhcEFmzRNbyI0EqLMAxIwceUKSQIDAQABo4GnMIGkMB0G\r\nA1UdDgQWBBSGmOdvSXKXclic5UOKPW35JLMEEjB1BgNVHSMEbjBsgBSGmOdvSXKX\r\nclic5UOKPW35JLMEEqFJpEcwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgTClNvbWUt\r\nU3RhdGUxITAfBgNVBAoTGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZIIJALwzrJEI\r\nBOaeMAwGA1UdEwQFMAMBAf8wDQYJKoZIhvcNAQEFBQADgYEAcPfWn49pgAX54ji5\r\nSiUPFFNCuQGSSTHh2I+TMrs1G1Mb3a0X1dV5CNLRyXyuVxsqhiM/H2veFnTz2Q4U\r\nwdY/kPxE19Auwcz9AvCkw7ol1LIlLfJvBzjzOjEpZJNtkXTx8ROSooNrDeJl3HyN\r\ncciS5hf80XzIFqwhzaVS9gmiyM8=");
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.GET);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0045");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("c2VjcmV0"); // "secret" base64
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authid");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns(DateTime.UtcNow.AddMinutes(5).ToString("yyyyMMddHHmmss"));
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("R");
            _commandMock.Setup(c => c.GetAccessMode()).Returns("R");
            _commandMock.Setup(c => c.GetURLCharset()).Returns("UTF-8");
            _commandMock.Setup(c => c.GetStringToSign(false, "UTF-8")).Returns("string-to-sign");
           
            var certificate = new X509Certificate2(dummyCertBytes);
            _verifierMock.Setup(v => v.GetCertificate(-1)).Returns(certificate);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.Null);            
        }

        [Test]
        public void CheckRequest_ValidRequest_WithSignature_0046_ReturnsSuccess()
        {
            byte[] dummyCertBytes = Convert.FromBase64String("MIICsDCCAhmgAwIBAgIJALwzrJEIBOaeMA0GCSqGSIb3DQEBBQUAMEUxCzAJBgNV\r\nBAYTAkFVMRMwEQYDVQQIEwpTb21lLVN0YXRlMSEwHwYDVQQKExhJbnRlcm5ldCBX\r\naWRnaXRzIFB0eSBMdGQwHhcNMTEwOTMwMTUyNjM2WhcNMjEwOTI3MTUyNjM2WjBF\r\nMQswCQYDVQQGEwJBVTETMBEGA1UECBMKU29tZS1TdGF0ZTEhMB8GA1UEChMYSW50\r\nZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKB\r\ngQC88Ckwru9VR2p2KJ1WQyqesLzr95taNbhkYfsd0j8Tl0MGY5h+dczCaMQz0YY3\r\nxHXuU5yAQQTZjiks+D3KA3cx+iKDf2p1q77oXxQcx5CkrXBWTaX2oqVtHm3aX23B\r\nAIORGuPk00b4rT3cld7VhcEFmzRNbyI0EqLMAxIwceUKSQIDAQABo4GnMIGkMB0G\r\nA1UdDgQWBBSGmOdvSXKXclic5UOKPW35JLMEEjB1BgNVHSMEbjBsgBSGmOdvSXKX\r\nclic5UOKPW35JLMEEqFJpEcwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgTClNvbWUt\r\nU3RhdGUxITAfBgNVBAoTGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZIIJALwzrJEI\r\nBOaeMAwGA1UdEwQFMAMBAf8wDQYJKoZIhvcNAQEFBQADgYEAcPfWn49pgAX54ji5\r\nSiUPFFNCuQGSSTHh2I+TMrs1G1Mb3a0X1dV5CNLRyXyuVxsqhiM/H2veFnTz2Q4U\r\nwdY/kPxE19Auwcz9AvCkw7ol1LIlLfJvBzjzOjEpZJNtkXTx8ROSooNrDeJl3HyN\r\ncciS5hf80XzIFqwhzaVS9gmiyM8=");
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.GET);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0046");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("c2VjcmV0"); // "secret" base64
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authid");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns(DateTime.UtcNow.AddMinutes(5).ToString("yyyyMMddHHmmss"));
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("crud");
            _commandMock.Setup(c => c.GetAccessMode()).Returns("crud");
            _commandMock.Setup(c => c.GetURLCharset()).Returns("UTF-8");
            _commandMock.Setup(c => c.GetStringToSign(false, "UTF-8")).Returns("string-to-sign");

            var certificate = new X509Certificate2(dummyCertBytes);
            _verifierMock.Setup(v => v.GetCertificate(-1)).Returns(certificate);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.Null);
        }

        [Test]
        public void CheckRequest_ValidRequest_WithSignature_0047_ReturnsSuccess()
        {
            byte[] dummyCertBytes = Convert.FromBase64String("MIICsDCCAhmgAwIBAgIJALwzrJEIBOaeMA0GCSqGSIb3DQEBBQUAMEUxCzAJBgNV\r\nBAYTAkFVMRMwEQYDVQQIEwpTb21lLVN0YXRlMSEwHwYDVQQKExhJbnRlcm5ldCBX\r\naWRnaXRzIFB0eSBMdGQwHhcNMTEwOTMwMTUyNjM2WhcNMjEwOTI3MTUyNjM2WjBF\r\nMQswCQYDVQQGEwJBVTETMBEGA1UECBMKU29tZS1TdGF0ZTEhMB8GA1UEChMYSW50\r\nZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKB\r\ngQC88Ckwru9VR2p2KJ1WQyqesLzr95taNbhkYfsd0j8Tl0MGY5h+dczCaMQz0YY3\r\nxHXuU5yAQQTZjiks+D3KA3cx+iKDf2p1q77oXxQcx5CkrXBWTaX2oqVtHm3aX23B\r\nAIORGuPk00b4rT3cld7VhcEFmzRNbyI0EqLMAxIwceUKSQIDAQABo4GnMIGkMB0G\r\nA1UdDgQWBBSGmOdvSXKXclic5UOKPW35JLMEEjB1BgNVHSMEbjBsgBSGmOdvSXKX\r\nclic5UOKPW35JLMEEqFJpEcwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgTClNvbWUt\r\nU3RhdGUxITAfBgNVBAoTGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZIIJALwzrJEI\r\nBOaeMAwGA1UdEwQFMAMBAf8wDQYJKoZIhvcNAQEFBQADgYEAcPfWn49pgAX54ji5\r\nSiUPFFNCuQGSSTHh2I+TMrs1G1Mb3a0X1dV5CNLRyXyuVxsqhiM/H2veFnTz2Q4U\r\nwdY/kPxE19Auwcz9AvCkw7ol1LIlLfJvBzjzOjEpZJNtkXTx8ROSooNrDeJl3HyN\r\ncciS5hf80XzIFqwhzaVS9gmiyM8=");
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.GET);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0047");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("c2VjcmV0"); // "secret" base64
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authid");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns(DateTime.UtcNow.AddMinutes(5).ToString("yyyyMMddHHmmss"));
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("crud");
            _commandMock.Setup(c => c.GetAccessMode()).Returns("crud");
            _commandMock.Setup(c => c.GetURLCharset()).Returns("UTF-8");
            _commandMock.Setup(c => c.GetStringToSign(false, "UTF-8")).Returns("string-to-sign");       
            
           var certificate = new X509Certificate2(dummyCertBytes);
            _verifierMock.Setup(v => v.GetCertificate(-1)).Returns(certificate);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.Null);
        }

        [Test]
        public void CheckRequest_ExpiredSignature_ReturnsFail()
        {
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.GET);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0045");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("c2VjcmV0");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authid");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns(DateTime.UtcNow.AddMinutes(-5).ToString("yyyyMMddHHmmss"));
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("crud");
            _commandMock.Setup(c => c.GetAccessMode()).Returns("crud");
            _commandMock.Setup(c => c.GetURLCharset()).Returns("UTF-8");
            _commandMock.Setup(c => c.GetStringToSign(false, "UTF-8")).Returns("string-to-sign");
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_errorResponseMock.Object);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.EqualTo(_errorResponseMock.Object));           
        }

        [Test]
        public void CheckRequest_ExpiredSignature_InvalidFormat_ReturnsFail()
        {
            _commandMock.Setup(c => c.GetTemplate()).Returns(ALCommandTemplate.GET);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("0045");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("c2VjcmV0");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authid");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("2025-07-04T12:34:56");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("crud");
            _commandMock.Setup(c => c.GetAccessMode()).Returns("crud");
            _commandMock.Setup(c => c.GetURLCharset()).Returns("UTF-8");
            _commandMock.Setup(c => c.GetStringToSign(false, "UTF-8")).Returns("string-to-sign");
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_errorResponseMock.Object);

            var result = _authenticator.CheckRequest(_request, _commandMock.Object, _certMock.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ErrorResponse, Is.EqualTo(_errorResponseMock.Object));
        }
    }
}