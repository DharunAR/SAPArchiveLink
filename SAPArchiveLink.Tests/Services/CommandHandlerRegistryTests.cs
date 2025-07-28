using NUnit.Framework;
using SAPArchiveLink;
using System;
using System.Collections.Generic;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class CommandHandlerRegistryTests
    {
        private Mock<ICommandHandler> _mockCreatePostHandler;
        private Mock<ICommandHandler> _mockDeletePostHandler;

        [SetUp]
        public void SetUp()
        {
            _mockCreatePostHandler = new Mock<ICommandHandler>();
            _mockCreatePostHandler.Setup(h => h.CommandTemplate).Returns(ALCommandTemplate.CREATEPOST);

            _mockDeletePostHandler = new Mock<ICommandHandler>();
            _mockDeletePostHandler.Setup(h => h.CommandTemplate).Returns(ALCommandTemplate.DELETE);
        }

        [Test]
        public void Constructor_ShouldRegisterHandlersSuccessfully()
        {
            // Arrange
            var handlers = new List<ICommandHandler> { _mockCreatePostHandler.Object, _mockDeletePostHandler.Object };

            // Act
            var registry = new CommandHandlerRegistry(handlers);

            // Assert
            var handler = registry.GetHandler(ALCommandTemplate.CREATEPOST);
            Assert.That(handler, Is.EqualTo(_mockCreatePostHandler.Object));
        }

        [Test]
        public void Constructor_ShouldThrowException_OnDuplicateCommandTemplate()
        {
            // Arrange
            var duplicateHandler = new Mock<ICommandHandler>();
            duplicateHandler.Setup(h => h.CommandTemplate).Returns(ALCommandTemplate.CREATEPOST);

            var handlers = new List<ICommandHandler>
            {
                _mockCreatePostHandler.Object,
                duplicateHandler.Object
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => new CommandHandlerRegistry(handlers));
            Assert.That(ex.Message, Does.Contain("Duplicate handler for command"));
        }

        [Test]
        public void GetHandler_ShouldReturnNull_IfHandlerNotFound()
        {
            // Arrange
            var registry = new CommandHandlerRegistry(new List<ICommandHandler>
            {
                _mockCreatePostHandler.Object
            });

            // Act
            var handler = registry.GetHandler(ALCommandTemplate.UPDATE_POST);

            // Assert
            Assert.That(handler, Is.Null);
        }
    }
}
