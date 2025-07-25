using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;
using SAPArchiveLink;
using SAPArchiveLink.Resources;

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
            // Arrange
            _commandMock.Setup(c => c.GetValue(ALParameter.VarFromOffset)).Returns("-1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarToOffset)).Returns("-2");
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError), Times.Once);           
        }

        [Test]
        public async Task HandleAsync_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Throws(new Exception("fail"));
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("fail")), StatusCodes.Status500InternalServerError), Times.Once);
            _loggerMock.Verify(l => l.LogError("Exception on AttrSearchCommandHandler", It.IsAny<Exception>()), Times.Once);
            Assert.That(result, Is.Not.Null);
        }
    }
}
