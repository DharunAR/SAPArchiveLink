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

            Assert.AreEqual("C:\\TRIM\\Work", trimConfig.WorkPath);
            Assert.AreEqual("C:\\TRIM\\Bin", trimConfig.BinariesLoadPath);
        }

        [Test]
        public void Should_Handle_Missing_Section_Gracefully()
        {
            var config = new ConfigurationBuilder().Build();
            var trimConfig = config.GetSection("TRIMConfig").Get<TrimConfigSettings>();

            Assert.IsNull(trimConfig?.WorkPath);
        }
    }
}
