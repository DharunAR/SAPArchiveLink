using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class BaseServicesTests
    {
        private Mock<ILogHelper<BaseServices>> _loggerMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IDatabaseConnection> _dbConnectionMock;
        private Mock<IDownloadFileHandler> _downloadFileHandlerMock;
        private Mock<ITrimRepository> _trimRepoMock;
        //private Mock<IBaseServices> _service;
        private BaseServices _service;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogHelper<BaseServices>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _dbConnectionMock = new Mock<IDatabaseConnection>();
            _trimRepoMock = new Mock<ITrimRepository>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();

            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(_trimRepoMock.Object);

            _service = new BaseServices(
                _loggerMock.Object,
                _responseFactoryMock.Object,
                _dbConnectionMock.Object,
                _downloadFileHandlerMock.Object);
        }

        [Test]
        public async Task PutCert_ReturnsError_WhenAuthIdMissing()
        {
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.PutCert(null, new MemoryStream(), "contRep", "crud");

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("authId")), StatusCodes.Status404NotFound), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task PutCert_ReturnsError_WhenContRepIdMissing()
        {
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.PutCert("auth", new MemoryStream(), null, "crud");

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("contRep")), StatusCodes.Status404NotFound), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task PutCert_ReturnsProtocolText_OnSuccess()
        {
            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.PutCert("auth", new MemoryStream(new byte[] { 1, 2, 3 }), "contRep", "crud");

            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.Is<string>(s => s.Contains("Certificate published")), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task DocGetSapComponents_ReturnsError_WhenValidationFails()
        {
            var sapDoc = new SapDocumentRequest { DocId = null, ContRep = null, PVersion = null };
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.DocGetSapComponents(sapDoc);

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GetSapDocument_ReturnsError_WhenRecordNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1" };
            var repoMock = new Mock<ITrimRepository>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.GetSapDocument(sapDoc);

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("Record not found")), StatusCodes.Status404NotFound), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task CreateRecord_ReturnsError_WhenValidationFails()
        {
            var model = new CreateSapDocumentModel { DocId = null, ContRep = null, CompId = null, PVersion = null, ContentLength = null };
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task CreateRecord_ReturnsProtocolText_OnSuccess()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = "comp",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponent>
                    {
                        new SapDocumentComponent { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
                    }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);
            _downloadFileHandlerMock.Setup(h => h.DownloadDocument(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("file.txt");
            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.Is<string>(s => s.Contains("Component(s) created successfully.")), StatusCodes.Status201Created, "UTF-8"), Times.Once);
            Assert.IsNotNull(result);
        }
    }
}