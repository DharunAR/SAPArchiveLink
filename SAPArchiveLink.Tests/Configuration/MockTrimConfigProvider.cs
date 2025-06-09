using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPArchiveLink.Tests
{
    public class MockTrimConfigProvider
    {
        private readonly TrimConfigSettings _mockConfig;

        public MockTrimConfigProvider(TrimConfigSettings mockConfig = null)
        {
            // Provide a default mock config if none is supplied
            _mockConfig = mockConfig ?? new TrimConfigSettings
            {
                DatabaseId = "P1",
                WGSName = "localhost",
                WGSPort = 1137,
                BinariesLoadPath = "C:\\Audit\\x64\\Debug",
                WorkPath = "",
                TrustedUser = "",
                WGSAlternateName = "",
                WGSAlternatePort = 0
            };
        }

        public TrimConfigSettings GetTrimConfig()
        {
            return _mockConfig;
        }
    }
}
