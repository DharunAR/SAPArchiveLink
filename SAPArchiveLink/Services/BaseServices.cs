using SAPArchiveLink;
using System.Net.Sockets;
using System.Security;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{
    private CMArchieveLinkClient cmClient;
    public BaseServices()
    {
            
    }
    public void PutCert(string authId, byte[] certificate, string contRepId, string permissions)
    {
        const string methodName = "PutCert";     

    // LC.Enter(methodName);

    string errorAuthId = "Parameter 'auth id' must not be null or empty.";
       // string titleAuthId = UALConstants.INVALID_PARAMETER;
        ArchiveCertificate icsArchiveCertificate;

        try
        {
            icsArchiveCertificate = ArchiveCertificate.FromByteArray(certificate);
        }
        catch (Exception ex) // RuntimeException | ICSException in Java
        {
            string certErrorMessage = "Bad Format of certificate. Got error: " + ex.Message;
            string errorTitle = "wrong format of certificate";
            //LC.Warn(methodName, certErrorMessage);
            //throw new ICSJDSException(ICSException.ERROR_DS_GENERIC, UALConstants.INVALID_CERTIFICATE_FORMAT,
            //                          UALConstants.HTTP_NOT_ACCEPTABLE_CODE, errorTitle, certErrorMessage,
            //                          new SecurityException(certErrorMessage));
        }

        string content64 = Convert.ToBase64String(certificate);

        if (string.IsNullOrEmpty(authId))
        {
           // LC.Warn(methodName, "Invalid authId");
            //throw new ICSJDSException(ICSException.INVALID_PARAMETER, UALConstants.INVALID_AUTH_ID,
            //                          UALConstants.HTTP_BAD_REQUEST, titleAuthId, errorAuthId,
            //                          new ArgumentException(errorAuthId));
        }

        int protectionLevel = -1;
        if (!string.IsNullOrEmpty(permissions))
        {
            protectionLevel = CommonUtils.ConvertProtection(permissions);
        }

        string archiveName = contRepId;
        long? archiveDataID = null;

        if (!string.IsNullOrWhiteSpace(archiveName))
        {
           // archiveDataID = GetArchiveIDByName(archiveName);
        }

        try
        {
            //csClient.PutArchiveCertificate(authId, icsArchiveCertificate, protectionLevel, content64, archiveDataID);
            //cmClient.PutArchiveCertificate();
        }
        catch (IOException e)
        {
          //  LC.Warn(methodName, e.Message);
            //throw new ICSException(ICSException.ERROR_WRAPPER, ICSException.ERROR_WRAPPER_STR,
            //                       new object[] { e.GetType().Name, e.Message }, e);
        }

       // LC.Leave(methodName);
    }
}
