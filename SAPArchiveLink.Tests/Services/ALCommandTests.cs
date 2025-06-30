using Microsoft.AspNetCore.Http;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ALCommandTests
    {
        [TestCase("get&compId=123&docId=456&Pversion=0045", ALCommandTemplate.GET, "123", "456", "0045", "GET")]
        [TestCase("create&compId=abc&docId=def&Pversion=0047", ALCommandTemplate.CREATEPUT, "abc", "def", "0047", "PUT")]
        [TestCase("docget&compId=1&docId=2&Pversion=0046", ALCommandTemplate.DOCGET, "1", "2", "0046", "GET")]
        public void ALCommand_WithValidUrl_ParsesTemplateAndParameters(
            string url, ALCommandTemplate expectedTemplate,
            string expectedCompId, string expectedDocId, string expectedPversion, string method)
        {
            var httpContext = new DefaultHttpContext();
            var request = new CommandRequest
            {
                Url = url,
                HttpMethod = method,
                Charset = "UTF-8",
                HttpRequest = httpContext.Request
            };

            var command = ALCommand.FromHttpRequest(request);

            Assert.That(command.IsValid, Is.True);
            Assert.That(command.GetTemplate(), Is.EqualTo(expectedTemplate));
            Assert.That(command.GetValue("compId"), Is.EqualTo(expectedCompId));
            Assert.That(command.GetValue("docId"), Is.EqualTo(expectedDocId));
            Assert.That(command.GetValue("Pversion"), Is.EqualTo(expectedPversion));
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
