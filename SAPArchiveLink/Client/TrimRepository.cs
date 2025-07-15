using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class TrimRepository : ITrimRepository
    {
        private const string EnabledStatus = "Enabled";
        private const string DisabledStatus = "Disabled";

        private readonly Database _db;
        private readonly TrimConfigSettings _trimConfig;
        private readonly ILoggerFactory _loggerFactory;

        public TrimRepository(Database database, TrimConfigSettings trimConfig, ILoggerFactory loggerFactory)
        {
            _db = database ?? throw new ArgumentNullException(nameof(database));
            _trimConfig = trimConfig ?? throw new ArgumentNullException(nameof(trimConfig));
            _loggerFactory = loggerFactory;
        }

        public IArchiveRecord? GetRecord(string docId, string contRep)
        {
            var tmos = new TrimMainObjectSearch(_db, BaseObjectTypes.Record);
            var docClause = new TrimSearchClause(_db, BaseObjectTypes.Record, SearchClauseIds.RecordSapdoc);
            docClause.SetCriteriaFromString(docId);
            tmos.AddSearchClause(docClause);
            var contRepClause = new TrimSearchClause(_db, BaseObjectTypes.Record, SearchClauseIds.RecordSaprepos);
            contRepClause.SetCriteriaFromString(contRep);
            tmos.AddSearchClause(contRepClause);

            if (tmos.Count > 0)
            {
                var uris = tmos.GetResultAsUriArray(1);
                var record = new Record(_db, uris[0]);
                return new ArchiveRecord(record, _trimConfig, new LogHelper<ArchiveRecord>(_loggerFactory.CreateLogger<ArchiveRecord>()));
            }

            return null;
        }

        public IArchiveRecord? CreateRecord(CreateSapDocumentModel model)
        {
            return ArchiveRecord.CreateNewArchiveRecord(_db, _trimConfig, new LogHelper<ArchiveRecord>(_loggerFactory.CreateLogger<ArchiveRecord>()), model);
        }

        public void Dispose() => _db.Dispose();

        /// <summary>
        /// Puts an archive certificate into the record.
        /// </summary>
        /// <param name="authId"></param>
        /// <param name="protectionLevel"></param>
        /// <param name="certificate"></param>
        /// <param name="contRep"></param>
        /// <returns></returns>
        public void SaveCertificate(string authId, int protectionLevel, IArchiveCertificate archiveCertificate, string contRep)
        {            
            string serialName = archiveCertificate.getSerialNumber();
            string fingerPrint = archiveCertificate.GetFingerprint();
            string issuer = archiveCertificate.getIssuerName();
            TrimDateTime validFrom = TrimDateTime.Parse(archiveCertificate.ValidFrom());
            TrimDateTime validTill = TrimDateTime.Parse(archiveCertificate.ValidTill());           
            var cert= archiveCertificate.GetCertificate();
            byte[] certBytes = cert.Export(X509ContentType.Cert);
            // Convert to Base64
            string base64Cert = Convert.ToBase64String(certBytes);

            SapRepoConfigUserOptions sapRepoConfigUserOptions = new SapRepoConfigUserOptions(_db);      

            SapRepoItem sapRepoItem = new SapRepoItem();

            sapRepoItem.setArchiveDataID(contRep);
            sapRepoItem.setAuthId(authId);
            sapRepoItem.setFingerPrint(fingerPrint);
            sapRepoItem.setContent(base64Cert);
            sapRepoItem.setIssuerCertificate(issuer);
            sapRepoItem.setPermissions(protectionLevel);
            sapRepoItem.setSerialName(serialName);
            sapRepoItem.setValidFrom(validFrom);
            sapRepoItem.setValidTill(validTill);

            if (GetArchiveCertificate(contRep) != null)
            {
                sapRepoConfigUserOptions.UpdateSapRepoItem(sapRepoItem);
            }
            else
            {
                sapRepoConfigUserOptions.AddSapRepoItem(sapRepoItem);
            }
            sapRepoConfigUserOptions.Save();            
        }

        public IArchiveCertificate GetArchiveCertificate(string contentRepo)
        {
            SapRepoConfigUserOptions sapRepoConfigUserOptions = new SapRepoConfigUserOptions(_db);
            SapRepoItem? item = FindSapRepoItem(contentRepo, sapRepoConfigUserOptions);
            if (item != null)
            {
                bool isEnabed = sapRepoConfigUserOptions.getIsEnabled(item.ArchiveDataID);
                byte[] certBytes = Convert.FromBase64String(item.Content);
                var certificate = new X509Certificate2(certBytes);
                return new ArchiveCertificate(certificate, item.AuthId, item.Permissions, isEnabed);
            }
            return null;
        }

        /// <summary>
        /// Retrieves server information and content repository details.
        /// </summary>
        /// <param name="protocolVersion">The protocol version of the client.</param>
        /// <param name="contentRepositoryId">The content repository ID to filter by, or empty to retrieve all repositories.</param>
        /// <returns>A ServerInfoModel containing server and repository details.</returns>
        public ServerInfoModel GetServerInfo(string pVersion, string contRep)
        {
            string serverVersion = TrimApplication.SoftwareVersion;
            string vendorId = TrimApplication.SDKVersion.ToString() ?? string.Empty;
            string buildNo = serverVersion.Contains('.') ? serverVersion.Split('.').LastOrDefault() ?? string.Empty : string.Empty;

            var infoModel = new ServerInfoModel
            {
                ServerVendorId = vendorId,
                ServerVersion = serverVersion,
                ServerBuild = buildNo,
                PVersion = pVersion,
                ServerDate = TrimDateTime.Now.ToDateTimeUTC().ToString("yyyyy-MM-dd"),
                ServerTime = TrimDateTime.Now.ToDateTimeUTC().ToString("HH:MM:ss")
            };

            var sapRepoConfigUserOptions = new SapRepoConfigUserOptions(_db);

            if (string.IsNullOrWhiteSpace(contRep))
            {
                foreach (var item in sapRepoConfigUserOptions.SapRepos)
                {
                    infoModel.ContentRepositories.Add(CreateContentRepositoryInfoModel(item, pVersion, sapRepoConfigUserOptions));
                }
            }
            else
            {
                var item = FindSapRepoItem(contRep, sapRepoConfigUserOptions);
                if (item != null)
                {
                    infoModel.ContentRepositories.Add(CreateContentRepositoryInfoModel(item, pVersion, sapRepoConfigUserOptions));
                }   
            }

            return infoModel;
        }

        public void SaveCounters(string archiveId, ArchiveCounter counter)
        {
            SapRepoCounters counters = new SapRepoCounters(_db);
            ApiSapRepoCounterItemList list = new ApiSapRepoCounterItemList();

            SapRepoCounter count = new SapRepoCounter();
            count.setArchiveDataID(archiveId);
            count.incrementCreateCounter(counter.CreateCount);
            count.incrementDeleteCounter(counter.DeleteCount);
            count.incrementUpdateCounter(counter.UpdateCount);
            count.incrementViewCounter(counter.ViewCount);
            list.Add(count);

            counters.IncrementCounters(list);
            counters.Save();          
        }

        private ContentRepositoryInfoModel CreateContentRepositoryInfoModel(SapRepoItem item, string pVersion, SapRepoConfigUserOptions sapRepoConfigUserOptions)
        {
            return new ContentRepositoryInfoModel
            {
                ContRep = item.ArchiveDataID,
                ContRepDescription = item.AuthId,
                ContRepStatus = sapRepoConfigUserOptions.getIsEnabled(item.ArchiveDataID) ? EnabledStatus : DisabledStatus,
                PVersion = pVersion
            };
        }

        private SapRepoItem? FindSapRepoItem(string contentRepo, SapRepoConfigUserOptions sapRepoConfigUserOptions)
        {
            ApiSapRepoItemList repoList = sapRepoConfigUserOptions.SapRepos;
            return repoList?.FirstOrDefault(item => item.ArchiveDataID == contentRepo);
        }
    }
}
