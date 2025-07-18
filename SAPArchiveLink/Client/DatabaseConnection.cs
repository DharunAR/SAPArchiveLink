using Microsoft.Extensions.Options;
using TRIM.SDK;

namespace SAPArchiveLink
{
    /// <summary>
    /// Implementation of the IDatabaseConnection interface for handling database connections.
    /// </summary>
    public class DatabaseConnection : IDatabaseConnection
    {
        private readonly TrimConfigSettings _trimConfig;
        private readonly ILoggerFactory _loggerFactory;

        public DatabaseConnection(IOptionsMonitor<TrimConfigSettings> config, ILoggerFactory loggerFactory)
        {
            _trimConfig = config.CurrentValue;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Creates and returns a new instance of ITrimRepository connected to the specified database.
        /// </summary>
        /// <returns></returns>
        public ITrimRepository GetDatabase()
        {
            var db = new Database
            {
                Id = _trimConfig.DatabaseId,
                WorkgroupServerName = _trimConfig.WGSName,
                WorkgroupServerPort = _trimConfig.WGSPort
            };

            if (!string.IsNullOrWhiteSpace(_trimConfig.TrustedUser))
                db.TrustedUser = _trimConfig.TrustedUser;

            if (!string.IsNullOrWhiteSpace(_trimConfig.WGSAlternateName))
            {
                db.AlternateWorkgroupServerName = _trimConfig.WGSAlternateName;
                db.AlternateWorkgroupServerPort = _trimConfig.WGSAlternatePort;
            }

            if (db.IsValid)             
                db.Connect();          
            return new TrimRepository(db, _trimConfig, _loggerFactory);
        }
    }

}