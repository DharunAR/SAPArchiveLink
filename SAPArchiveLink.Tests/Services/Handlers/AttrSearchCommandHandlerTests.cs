using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class AttrSearchCommandHandlerTests
    {
        private Mock<ILogHelper<AttrSearchCommandHandler>> _loggerMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServicesMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private AttrSearchCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogHelper<AttrSearchCommandHandler>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServicesMock = new Mock<IBaseServices>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _handler = new AttrSearchCommandHandler(
                _loggerMock.Object,
                _responseFactoryMock.Object,
                _baseServicesMock.Object
            );
        }

        [Test]
        public void CommandTemplate_Returns_ATTRSEARCH()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.ATTRSEARCH));
        }

        [Test]
        public async Task HandleAsync_ValidRequest_ReturnsBaseServiceResult()
        {
            // Arrange
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("rep");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("docid");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns((string)null);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPattern)).Returns("pattern");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("v1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCaseSensitive)).Returns("y");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarFromOffset)).Returns("0");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarToOffset)).Returns("10");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarNumResults)).Returns("5");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("mode");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("auth");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("exp");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("key");

            var expectedResponse = Mock.Of<ICommandResponse>();
            _baseServicesMock.Setup(s => s.GetAttrSearchResult(It.IsAny<SapSearchRequestModel>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(expectedResponse, Is.EqualTo(result));         
            _baseServicesMock.Verify(s => s.GetAttrSearchResult(It.IsAny<SapSearchRequestModel>()), Times.Once);
        }

        [Test]
        public async Task HandleAsync_InvalidOffset_ReturnsErrorResponse()
        {
            _commandMock.Setup(c => c.GetValue(ALParameter.VarFromOffset)).Returns("-1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarToOffset)).Returns("-2");
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                .Returns(Mock.Of<ICommandResponse>());

            await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ExceptionThrown_ReturnsInternalServerError()
        {
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Throws(new Exception("fail"));
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("fail")), StatusCodes.Status500InternalServerError), Times.Once);
            _loggerMock.Verify(l => l.LogError("Exception on AttrSearchCommandHandler", It.IsAny<Exception>()), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task HandleAsync_DefaultsCompIdToDescr_WhenNull()
        {
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns((string)null);
            SapSearchRequestModel capturedRequest = null!;
            _baseServicesMock.Setup(s => s.GetAttrSearchResult(It.IsAny<SapSearchRequestModel>()))
                .Callback<SapSearchRequestModel>(r => capturedRequest = r)
                .ReturnsAsync(Mock.Of<ICommandResponse>());

            await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(capturedRequest.CompId, Is.EqualTo("descr"));
        }

        [Test]
        public async Task HandleAsync_DefaultsNumResultsTo1_WhenInvalid()
        {
            _commandMock.Setup(c => c.GetValue(ALParameter.VarNumResults)).Returns("invalid");
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Returns("dummy");

            SapSearchRequestModel capturedRequest = null!;
            _baseServicesMock.Setup(s => s.GetAttrSearchResult(It.IsAny<SapSearchRequestModel>()))
                .Callback<SapSearchRequestModel>(r => capturedRequest = r)
                .ReturnsAsync(Mock.Of<ICommandResponse>());

            await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(capturedRequest.NumResults, Is.EqualTo(1));
        }

        [TestCase("n", false)]
        [TestCase(null, false)]
        public async Task HandleAsync_CaseSensitiveParsing_WorksCorrectly(string? input, bool expected)
        {
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCaseSensitive)).Returns(input);
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Returns("dummy"); // fallback for other params

            _baseServicesMock.Setup(s => s.GetAttrSearchResult(It.Is<SapSearchRequestModel>(
                r => r.CaseSensitive == expected))).ReturnsAsync(Mock.Of<ICommandResponse>());

            await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _baseServicesMock.Verify(s => s.GetAttrSearchResult(It.IsAny<SapSearchRequestModel>()), Times.Once);
        }
    }
}
