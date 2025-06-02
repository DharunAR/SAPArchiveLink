using TRIM.SDK;

namespace SAPArchiveLink
{
    public static class DatabaseConnection
    {
        static DatabaseConnection()
        {
            Database.AllowAccessFromMultipleThreads = true;
        }

        public static Database GetDatabase(TrimConfigSettings trimConfig)
        {
            var db = new Database
            {
                Id = trimConfig.DatabaseId,
                WorkgroupServerName = trimConfig.WGSName,
                WorkgroupServerPort = trimConfig.WGSPort
            };

            if (!string.IsNullOrWhiteSpace(trimConfig.TrustedUser))
            {
                db.TrustedUser = trimConfig.TrustedUser;
            }

            if (!string.IsNullOrWhiteSpace(trimConfig.WGSAlternateName))
            {
                db.AlternateWorkgroupServerName = trimConfig.WGSAlternateName;
                db.AlternateWorkgroupServerPort = trimConfig.WGSAlternatePort;
            }

            if (db.IsValid)
            {
                db.Connect();
            }

            return db;
        }

    }
}
