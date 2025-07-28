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
            trimInit.TrimInitialized();

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
        public void FlushCounters_ShouldNotFlush_WhenTrimNotInitialized()
        {
            var cacheMock = new Mock<ICounterCache>();
            var repoMock = new Mock<ITrimRepository>();
            var flusherLoggerMock = new Mock<ILogHelper<CounterFlusher>>();
            var loggerMock = new Mock<ILogHelper<CounterFlushHostedService>>();

            var trimInit = new TrimInitialization();
            trimInit.FailInitialization("Trim not initialized"); // Set isInitialized = false

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(ICounterCache))).Returns(cacheMock.Object);
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
                .GetMethod("FlushCounters", BindingFlags.Instance | BindingFlags.NonPublic);

            flushMethod!.Invoke(hostedService, new object[] { null });
            repoMock.Verify(r => r.SaveCounters(It.IsAny<string>(), It.IsAny<ArchiveCounter>()), Times.Never);
            cacheMock.Verify(c => c.Reset(It.IsAny<string>()), Times.Never);
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

        [Test]
        public void FlushCounters_LogsError_WhenExceptionIsThrown()
        {
            var loggerMock = new Mock<ILogHelper<CounterFlushHostedService>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(CounterFlusher)))
                               .Throws(new Exception("Simulated failure"));

            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            var hostedService = new CounterFlushHostedService(loggerMock.Object, scopeFactoryMock.Object);

            var flushMethod = typeof(CounterFlushHostedService)
                .GetMethod("FlushCounters", BindingFlags.Instance | BindingFlags.NonPublic);

            flushMethod!.Invoke(hostedService, new object[] { null });

            loggerMock.Verify(l => l.LogError(It.Is<string>(msg => msg.Contains("Flush Counter process failed")), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var loggerMock = new Mock<ILogHelper<CounterFlushHostedService>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();

            var hostedService = new CounterFlushHostedService(loggerMock.Object, scopeFactoryMock.Object);

            hostedService.Dispose();
            hostedService.Dispose(); // Should not throw

            Assert.Pass("Dispose is idempotent.");
        }

        [Test]
        public async Task StopAsync_DoesNotThrow_WhenTimerIsNull()
        {
            var loggerMock = new Mock<ILogHelper<CounterFlushHostedService>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();

            var hostedService = new CounterFlushHostedService(loggerMock.Object, scopeFactoryMock.Object);

            // Simulate timer already disposed
            await hostedService.StopAsync(CancellationToken.None);

            Assert.Pass("StopAsync executed without error when timer is null.");
        }


    }
}
