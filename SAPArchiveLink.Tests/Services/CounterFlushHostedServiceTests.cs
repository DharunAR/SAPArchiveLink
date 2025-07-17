using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;


namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class CounterFlushHostedServiceTests
    {  
        [Test]
        public void CounterFlushHostedService_ShouldCallFlush()
        {
            // Arrange
            var cacheMock = new Mock<ICounterCache>();
            var dbConnMock = new Mock<IDatabaseConnection>();
            var repoMock = new Mock<ITrimRepository>();
            var flusherLoggerMock = new Mock<ILogHelper<CounterFlusher>>();
            var loggerMock = new Mock<ILogHelper<CounterFlushHostedService>>();
            var counter = new ArchiveCounter();
            counter.IncrementCreate(1);

            var trimInit = new TrimInitialization();
            typeof(TrimInitialization).GetProperty(nameof(TrimInitialization.IsInitialized))!
                .SetValue(trimInit, true);

            cacheMock.Setup(c => c.GetAll()).Returns(new Dictionary<string, ArchiveCounter>
                {
                    { "R5", counter }
                });

            cacheMock.Setup(c => c.Reset("R5"));
            dbConnMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.SaveCounters("R5", It.IsAny<ArchiveCounter>()));

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(ICounterCache))).Returns(cacheMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(IDatabaseConnection))).Returns(dbConnMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(TrimInitialization))).Returns(trimInit);
            serviceProviderMock.Setup(x => x.GetService(typeof(ILogHelper<CounterFlusher>))).Returns(flusherLoggerMock.Object);

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            serviceProviderMock.Setup(x => x.GetService(typeof(CounterFlusher)))
             .Returns(new CounterFlusher(cacheMock.Object, scopeFactoryMock.Object, trimInit, flusherLoggerMock.Object));

            var hostedService = new CounterFlushHostedService(loggerMock.Object, scopeFactoryMock.Object);
       
            var flushMethod = typeof(CounterFlushHostedService)
      .GetMethod("FlushCounters", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);

            flushMethod!.Invoke(hostedService, new object[] { null });
          
            repoMock.Verify(r => r.SaveCounters("R5", It.IsAny<ArchiveCounter>()), Times.Once);
            cacheMock.Verify(c => c.Reset("R5"), Times.Once);
        }

        [Test]
        public async Task StartStop_ShouldNotThrow()
        {
            var loggerMock = new Mock<ILogHelper<CounterFlushHostedService>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();

            var hostedService = new CounterFlushHostedService(
                loggerMock.Object,
                scopeFactoryMock.Object
            );

            await hostedService.StartAsync(CancellationToken.None);
            await hostedService.StopAsync(CancellationToken.None);
            hostedService.Dispose();

            Assert.Pass("Start/Stop/Dispose executed without error.");
        }
    }
    public class TestTrimInitialization : TrimInitialization
    {
        public void SetInitialized(bool value)
        {
            typeof(TrimInitialization)
                .GetProperty(nameof(IsInitialized))
                .SetValue(this, value);
        }
    }
}
