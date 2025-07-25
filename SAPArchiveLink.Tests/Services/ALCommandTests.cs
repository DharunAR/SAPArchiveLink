using Microsoft.AspNetCore.Http;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ALCommandTests
    {
        [TestCase("get&compId=123&docId=456&Pversion=0045", ALCommandTemplate.GET, "123", "456", "0045", "GET", "r")]
        [TestCase("create&compId=abc&docId=def&Pversion=0047", ALCommandTemplate.CREATEPUT, "abc", "def", "0047", "PUT", "c")]
        [TestCase("create&compId=xyz&docId=789&Pversion=0046", ALCommandTemplate.CREATEPOST, "xyz", "789", "0046", "POST", "c")]
        [TestCase("docget&compId=1&docId=2&Pversion=0046", ALCommandTemplate.DOCGET, "1", "2", "0046", "GET", "r")]
        [TestCase("mcreate&compId=comp123&docId=doc456&Pversion=0045", ALCommandTemplate.MCREATE, "comp123", "doc456", "0045", "POST", "c")]
        [TestCase("append&compId=comp789&docId=doc012&Pversion=0047", ALCommandTemplate.APPEND, "comp789", "doc012", "0047", "PUT", "u")]
        [TestCase("update&compId=comp345&docId=doc678&Pversion=0046", ALCommandTemplate.UPDATE_PUT, "comp345", "doc678", "0046", "PUT", "u")]
        [TestCase("delete&compId=comp901&docId=doc234&Pversion=0045", ALCommandTemplate.DELETE, "comp901", "doc234", "0045", "GET", "d")]
        [TestCase("search&compId=comp567&docId=doc890&Pversion=0047", ALCommandTemplate.SEARCH, "comp567", "doc890", "0047", "GET", "r")]
        [TestCase("attrSearch&compId=comp123&docId=doc456&Pversion=0046", ALCommandTemplate.ATTRSEARCH, "comp123", "doc456", "0046", "GET", "r")]
        [TestCase("update&compId=comp789&docId=doc012&Pversion=0045", ALCommandTemplate.UPDATE_POST, "comp789", "doc012", "0045", "POST", "u")]
        public void ALCommand_WithValidUrl_ParsesTemplateAndParameters(
            string url, ALCommandTemplate expectedTemplate,
            string expectedCompId, string expectedDocId, string expectedPversion, string method,string accessMode)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            var request = new CommandRequest
            {
                Url = url,
                HttpMethod = method,
                Charset = "UTF-8",
                HttpRequest = httpContext.Request,
            };
           

            var command = ALCommand.FromHttpRequest(request);
            command.SetVerified();
            int index = url.IndexOf('&');
            string queryString = index >= 0 ? url.Substring(index + 1) : url;

            Assert.That(command.IsValid, Is.True);
            Assert.That(command.GetTemplate(), Is.EqualTo(expectedTemplate));
            Assert.That(command.GetValue("compId"), Is.EqualTo(expectedCompId));
            Assert.That(command.GetValue("docId"), Is.EqualTo(expectedDocId));
            Assert.That(command.GetValue("Pversion"), Is.EqualTo(expectedPversion));
            Assert.That(command.GetURLCharset, Is.EqualTo("UTF-8"));
            Assert.That(command.GetStringToSign(true, "UTF-8"), Is.EqualTo("https://?"+ queryString));
            Assert.That(command.IsVerified, Is.True);
            Assert.That(command.GetAccessMode, Is.EqualTo(accessMode));
        }

        [Test]
        public void ALCommand_WithUnknownCommand_SetsValidationError()
        {
            var request = new CommandRequest
            {
                Url = "invalidcmd&compId=1",
                HttpMethod = "GET",
                Charset = "UTF-8"
            };

            var command = ALCommand.FromHttpRequest(request);

            Assert.That(command.IsValid, Is.False);
            Assert.That(command.ValidationError, Does.Contain("Unsupported command"));
        }

        [Test]
        public void ALCommand_UnknownCommand_SetsValidationError()
        {
            var request = new CommandRequest
            {
                Url = "invalidcmd&compId=1",
                HttpMethod = "GET",
                Charset = "UTF-8"
            };

            var command = new ALCommand(request);

            Assert.That(command.IsValid, Is.False); 
            Assert.That(command.ValidationError, Does.Contain("Unsupported command"));
        }

        [Test]
        public void ALCommand_WithValidCommand_ReturnsValidCommand()
        {
            var request = new CommandRequest
            {
                Url = "get&docId=1",
                HttpMethod = "GET",
                Charset = "UTF-8"
            };

            var command = ALCommand.FromHttpRequest(request);

            Assert.That(command.IsValid, Is.True);
            Assert.That(command.ValidationError, Is.Null.Or.Empty);
        }

        [Test]
        public void ALCommand_ParsesAdditionalParameters()
        {
            var request = new CommandRequest
            {
                Url = "get&docId=1234&archivId=5678",
                HttpMethod = "GET",
                Charset = "UTF-8"
            };

            var command = ALCommand.FromHttpRequest(request);

            Assert.That(command.IsValid, Is.True);
            Assert.That(command.GetValue("docId"), Is.EqualTo("1234"));
            Assert.That(command.GetValue("archivId"), Is.EqualTo("5678"));
        }

        [Test]
        public void ALCommand_CreatesValidInstance_WhenCommandIsValid()
        {
            var request = new CommandRequest
            {
                Url = "get&docId=1",
                HttpMethod = "GET",
                Charset = "UTF-8"
            };

            var command = ALCommand.FromHttpRequest(request);

            Assert.That(command, Is.Not.Null);
            Assert.That(command.IsValid, Is.True);
        }
    }
}
