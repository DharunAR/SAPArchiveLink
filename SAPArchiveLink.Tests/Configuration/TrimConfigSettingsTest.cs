using Microsoft.Extensions.Configuration;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class TrimConfigSettingsTest
    {
        [Test]
        public void Should_Bind_TrimConfigSettings_From_Configuration()
        {
            var settings = new Dictionary<string, string>
            {
                { "TRIMConfig:WorkPath", "C:\\TRIM\\Work" },
                { "TRIMConfig:BinariesLoadPath", "C:\\TRIM\\Bin" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var trimConfig = config.GetSection("TRIMConfig").Get<TrimConfigSettings>();

            Assert.That(trimConfig?.WorkPath, Is.EqualTo("C:\\TRIM\\Work"));
            Assert.That(trimConfig?.BinariesLoadPath, Is.EqualTo("C:\\TRIM\\Bin"));
        }

        [Test]
        public void Should_Handle_Missing_Section_Gracefully()
        {
            var config = new ConfigurationBuilder().Build();
            var trimConfig = config.GetSection("TRIMConfig").Get<TrimConfigSettings>();

            Assert.That(trimConfig?.WorkPath, Is.Null);
        }
    }
}
