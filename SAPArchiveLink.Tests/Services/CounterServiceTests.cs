using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class CounterServiceTests
    {
        private Mock<ICounterCache> _cacheMock;
        private Mock<ILogHelper<CounterService>> _logHelperMock;
        private CounterService _service;
        private ArchiveCounter _testCounter;

        [SetUp]
        public void SetUp()
        {
            _cacheMock = new Mock<ICounterCache>();
            _logHelperMock = new Mock<ILogHelper<CounterService>>();
            _testCounter = new ArchiveCounter();

            _cacheMock.Setup(c => c.GetOrCreate(It.IsAny<string>()))
                      .Returns(_testCounter);

            _service = new CounterService(_cacheMock.Object, _logHelperMock.Object);
        }

        [Test]
        public void UpdateCounter_ShouldIncrementCreate_WhenTypeIsCreate()
        {
            _service.UpdateCounter("R1", CounterType.Create, 5);

            Assert.That(_testCounter.CreateCount, Is.EqualTo(5));
        }

        [Test]
        public void UpdateCounter_ShouldIncrementDelete_WhenTypeIsDelete()
        {
            _service.UpdateCounter("R1", CounterType.Delete, 3);

            Assert.That(_testCounter.DeleteCount, Is.EqualTo(3));
        }

        [Test]
        public void UpdateCounter_ShouldIncrementUpdate_WhenTypeIsUpdate()
        {
            _service.UpdateCounter("R1", CounterType.Update, 2);

            Assert.That(_testCounter.UpdateCount, Is.EqualTo(2));
        }

        [Test]
        public void UpdateCounter_ShouldIncrementView_WhenTypeIsView()
        {
            _service.UpdateCounter("R1", CounterType.View, 4);

            Assert.That(_testCounter.ViewCount, Is.EqualTo(4));
        }

        [Test]
        public void UpdateCounter_ShouldNotUpdate_WhenValueIsZero()
        {
            _service.UpdateCounter("R1", CounterType.Create, 0);

            Assert.That(_testCounter.CreateCount, Is.EqualTo(0));
        }

        [Test]
        public void UpdateCounter_ShouldNotUpdate_WhenValueIsNegative()
        {
            _service.UpdateCounter("R1", CounterType.Delete, -1);

            Assert.That(_testCounter.DeleteCount, Is.EqualTo(0));
        }

        [Test]
        public void UpdateCounter_ShouldThrow_WhenInvalidCounterType()
        {
            Assert.That(() => _service.UpdateCounter("R1", (CounterType)999, 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
