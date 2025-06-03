using TRIM.SDK;

namespace SAPArchiveLink
{
    /// <summary>
    /// Implementation of the IDatabaseConnection interface for handling database connections.
    /// </summary>
    public class DatabaseConnection: IDatabaseConnection
    {       
        private readonly TrimConfigSettings _trimConfig;

        public DatabaseConnection(TrimConfigSettings config)
        {
            _trimConfig = config;
            Database.AllowAccessFromMultipleThreads = true;
        }

        /// <summary>
        /// Creates and returns a database connection based on the configuration settings.
        /// </summary>
        /// <returns></returns>
        public Database GetDatabase()
        {
            var db = new Database
            {
                Id = _trimConfig.DatabaseId,
                WorkgroupServerName = _trimConfig.WGSName,
                WorkgroupServerPort = _trimConfig.WGSPort
            };

            if (!string.IsNullOrWhiteSpace(_trimConfig.TrustedUser))
            {
                db.TrustedUser = _trimConfig.TrustedUser;
            }

            if (!string.IsNullOrWhiteSpace(_trimConfig.WGSAlternateName))
            {
                db.AlternateWorkgroupServerName = _trimConfig.WGSAlternateName;
                db.AlternateWorkgroupServerPort = _trimConfig.WGSAlternatePort;
            }

            if (db.IsValid)
            {
                db.Connect();
            }

            return db;
        }
              
    }
}
