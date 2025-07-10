using Moq;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class MCreateCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _mockResponseFactory;
        private Mock<IBaseServices> _mockBaseServices;
        private Mock<IDownloadFileHandler> _mockDownloadFileHandler;
        private MCreateCommandHandler _handler;

        [SetUp]
        public void Setup()
        {
            _mockResponseFactory = new Mock<ICommandResponseFactory>();
            _mockBaseServices = new Mock<IBaseServices>();
            _mockDownloadFileHandler = new Mock<IDownloadFileHandler>();

            _handler = new MCreateCommandHandler(
            _mockResponseFactory.Object,
            _mockBaseServices.Object,
            _mockDownloadFileHandler.Object
            );
        }

        [Test]
        public void CommandTemplate_Returns_MCREATE()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.MCREATE));
        }

        [Test]
        public async Task HandleAsync_WithHttpRequest_ReturnsSuccess()
        {
            var command = new Mock<ICommand>();
            var context = new Mock<ICommandRequestContext>();
            var httpRequest = new Mock<HttpRequest>();

            var headers = new HeaderDictionary
 {
 { "charset", "UTF-8" },
 { "version", "1.0" }
 };

            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy body"));

            httpRequest.Setup(r => r.ContentType).Returns("multipart/form-data; boundary=----WebKitFormBoundary");
            httpRequest.Setup(r => r.Body).Returns(bodyStream);
            httpRequest.Setup(r => r.Headers).Returns(headers);
            httpRequest.Setup(r => r.ContentLength).Returns(1024);

            context.Setup(c => c.GetRequest()).Returns(httpRequest.Object);

            var components = new List<SapDocumentComponentModel>
 {
 new SapDocumentComponentModel
 {
 DocId = "doc123",
 CompId = "comp1",
 ContentType = "application/pdf",
 Charset = "UTF-8",
 FileName = "file1.pdf",
 PVersion = "1.0",
 ContentLength = 1024,
 Data = new MemoryStream(new byte[] { 0x01, 0x02 })
 }
 };

            _mockDownloadFileHandler
            .Setup(h => h.HandleRequestAsync(It.IsAny<string>(), It.IsAny<Stream>(), null))
            .ReturnsAsync(components);

            command.Setup(c => c.GetValue(It.IsAny<string>())).Returns("value");

            _mockBaseServices
            .Setup(s => s.CreateRecord(It.IsAny<CreateSapDocumentModel>(), true))
            .ReturnsAsync(Mock.Of<ICommandResponse>(r => r.StatusCode == 201 && r.TextContent == "Created"));

            _mockResponseFactory
            .Setup(f => f.CreateProtocolText(It.IsAny<string>(), StatusCodes.Status201Created, "UTF-8"))
            .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(command.Object, context.Object);

            Assert.That(result, Is.Not.Null);
            _mockDownloadFileHandler.Verify(h => h.HandleRequestAsync(It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
            _mockBaseServices.Verify(s => s.CreateRecord(It.IsAny<CreateSapDocumentModel>(), true), Times.Once);
        }

        [Test]
        public async Task HandleAsync_WhenExceptionThrown_ReturnsErrorResponse()
        {
            // Arrange
            var command = new Mock<ICommand>();
            var context = new Mock<ICommandRequestContext>();
            var httpRequest = new Mock<HttpRequest>();

            var headers = new HeaderDictionary
    {
        { "charset", "UTF-8" },
        { "version", "1.0" },
        { "docprot", "A" }
    };

            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes("invalid body"));

            httpRequest.Setup(r => r.ContentType).Returns("multipart/form-data; boundary=----InvalidBoundary");
            httpRequest.Setup(r => r.Body).Returns(bodyStream);
            httpRequest.Setup(r => r.Headers).Returns(headers);
            httpRequest.Setup(r => r.ContentLength).Returns(1024);

            context.Setup(c => c.GetRequest()).Returns(httpRequest.Object);

            _mockDownloadFileHandler
                .Setup(h => h.HandleRequestAsync(It.IsAny<string>(), It.IsAny<Stream>(), null))
                .ThrowsAsync(new InvalidOperationException("Boundary not found"));

            _mockResponseFactory
                .Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(command.Object, context.Object);

            Assert.That(result, Is.Not.Null);
            _mockResponseFactory.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("Internal server error")), StatusCodes.Status500InternalServerError),
                Times.Once);
        }

        [Test]
        public async Task HandleAsync_WithNullContentLengthAndCommandDocProt_ReturnsSuccess()
        {
            // Arrange
            var command = new Mock<ICommand>();
            var context = new Mock<ICommandRequestContext>();
            var httpRequest = new Mock<HttpRequest>();

            var headers = new HeaderDictionary
    {
        { "charset", "UTF-8" },
        { "version", "1.0" }
    };

            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));

            httpRequest.Setup(r => r.ContentType).Returns("multipart/form-data");
            httpRequest.Setup(r => r.Body).Returns(bodyStream);
            httpRequest.Setup(r => r.Headers).Returns(headers);
            httpRequest.Setup(r => r.ContentLength).Returns((long?)null); // simulate null

            context.Setup(c => c.GetRequest()).Returns(httpRequest.Object);

            var components = new List<SapDocumentComponentModel>
    {
        new SapDocumentComponentModel { DocId = "docX", CompId = "compX", Data = new MemoryStream() }
    };

            _mockDownloadFileHandler
                .Setup(h => h.HandleRequestAsync(It.IsAny<string>(), It.IsAny<Stream>(), null))
                .ReturnsAsync(components);

            // Setup command values
            command.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("ContRepVal");
            command.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("PVer");
            command.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("Access");
            command.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("Auth");
            command.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("Exp");
            command.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("Sec");
            command.Setup(c => c.GetValue(ALParameter.VarDocProt)).Returns("DocProtVal");

            _mockBaseServices
                .Setup(s => s.CreateRecord(It.IsAny<CreateSapDocumentModel>(), true))
                .ReturnsAsync(Mock.Of<ICommandResponse>(r => r.StatusCode == 201 && r.TextContent == "Created"));

            _mockResponseFactory
                .Setup(f => f.CreateProtocolText(It.IsAny<string>(), StatusCodes.Status201Created, "UTF-8"))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(command.Object, context.Object);

            Assert.That(result, Is.Not.Null);
            _mockBaseServices.Verify(s => s.CreateRecord(It.Is<CreateSapDocumentModel>(m =>
                m.DocId == "docX" &&
                m.ContRep == "ContRepVal" &&
                m.PVersion == "PVer" &&
                m.AccessMode == "Access" &&
                m.AuthId == "Auth" &&
                m.Expiration == "Exp" &&
                m.SecKey == "Sec" &&
                m.Charset == "UTF-8" &&
                m.Version == "1.0" &&
                m.DocProt == "DocProtVal" &&
                m.ContentType == "multipart/form-data" &&
                m.ContentLength == "0"
            ), true), Times.Once);
        }

    }
}
