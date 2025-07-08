using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class TrimRepository : ITrimRepository
    {
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
            ApiSapRepoItemList list = sapRepoConfigUserOptions.SapRepos;
            foreach (SapRepoItem item in list)
            {
                if (item.ArchiveDataID == contentRepo)
                {
                   bool isEnabed= sapRepoConfigUserOptions.getIsEnabled(item.ArchiveDataID);
                    byte[] certBytes = Convert.FromBase64String(item.Content);
                    var certificate = new X509Certificate2(certBytes);
                    return new ArchiveCertificate(certificate, item.AuthId,item.Permissions, isEnabed);
                }
            }
            return null;
        }
    }
}
