using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using SAPArchiveLink;
using SAPArchiveLink.Resources;

namespace SAPArchiveLink.Tests.Services.Handlers
{
    [TestFixture]
    public class SearchCommandHandlerTests
    {
        private Mock<ILogHelper<SearchCommandHandler>> _loggerMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServicesMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private SearchCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {            
            _loggerMock = new Mock<ILogHelper<SearchCommandHandler>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServicesMock = new Mock<IBaseServices>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();

            _handler = new SearchCommandHandler(
                _loggerMock.Object,
                _responseFactoryMock.Object,
                _baseServicesMock.Object
            );
        }

        [Test]
        public async Task HandleAsync_ReturnsSearchResult_WhenBaseServicesSucceeds()
        {
            // Arrange
            var expectedResponse = Mock.Of<ICommandResponse>();
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("ContRep1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("DocId1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPattern)).Returns("Pattern1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("CompId1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("PVersion1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCaseSensitive)).Returns("1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarFromOffset)).Returns("5");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarToOffset)).Returns("10");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarNumResults)).Returns("2");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("AccessMode1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("AuthId1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("Expiration1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("SecKey1");

            _baseServicesMock
                .Setup(s => s.GetSearchResult(It.IsAny<SapSearchRequestModel>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(expectedResponse, Is.EqualTo(result));
            _baseServicesMock.Verify(s => s.GetSearchResult(It.Is<SapSearchRequestModel>(r =>
                r.ContRep == "ContRep1" &&
                r.DocId == "DocId1" &&
                r.Pattern == "Pattern1" &&
                r.CompId == "CompId1" &&
                r.PVersion == "PVersion1" &&
                r.CaseSensitive == true &&
                r.FromOffset == 5 &&
                r.ToOffset == 10 &&
                r.NumResults == 2 &&
                r.AccessMode == "AccessMode1" &&
                r.AuthId == "AuthId1" &&
                r.Expiration == "Expiration1" &&
                r.SecKey == "SecKey1"
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_WhenExceptionThrown()
        {
            // Arrange
            var exception = new Exception("Test exception");
            _baseServicesMock
                .Setup(s => s.GetSearchResult(It.IsAny<SapSearchRequestModel>()))
                .ThrowsAsync(exception);

            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Returns((string)null);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateError(It.Is<string>(msg => msg.Contains("Test exception")), StatusCodes.Status500InternalServerError))
                .Returns(errorResponse);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
          //  Assert.That(errorResponse, Is.EqualTo(result));
            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError), Times.Once);
        }
    }
}
