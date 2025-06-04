using SAPArchiveLink;
using System.Net.Sockets;
using System.Security;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{   
    private readonly ILogHelper<BaseServices> _logger;  
    ICMArchieveLinkClient _cmArchieveLinkClient;
    IArchiveCertificate _archiveCertificate;
    private ICommandResponseFactory _commandResponseFactory;

    public BaseServices(ILogHelper<BaseServices> helperLogger, ICMArchieveLinkClient cmArchieveLinkClient, IArchiveCertificate archiveCertificate, ICommandResponseFactory commandResponseFactory)
    {
        _archiveCertificate = archiveCertificate;
        _cmArchieveLinkClient = cmArchieveLinkClient;
        _logger = helperLogger;
        _commandResponseFactory = commandResponseFactory;
    }

    /// <summary>
    /// Handles the SAP ArchiveLink 'putCert' command.
    /// </summary>
    /// <param name="authId"></param>
    /// <param name="inputStream"></param>
    /// <param name="contRepId"></param>
    /// <param name="permissions"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> PutCert(string authId, Stream inputStream, string contRepId, string permissions)
    {
        try
        {
            const string MN = "PutCert";
            if (string.IsNullOrWhiteSpace(authId))
            {
                _logger.LogError($"{MN} - Missing required parameter: authId");
                //need to look at the error code here,  it is a 404 error
                return _commandResponseFactory.CreateError("Missing required parameter: authId", StatusCodes.Status404NotFound);
            }
            if (string.IsNullOrWhiteSpace(contRepId))
            {
                _logger.LogError($"{MN} - Missing required parameter: contRep");
                return _commandResponseFactory.CreateError("\"Parameter 'contRep' must not be null or empty", StatusCodes.Status404NotFound);
            }

            using var memoryStream = new MemoryStream();
            byte[] buffer = new byte[1024 * 2]; // 2 KB buffer
            int bytesRead;

            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
            }

            int protectionLevel = -1;
            if (!string.IsNullOrWhiteSpace(permissions))
            {
                protectionLevel = SecurityUtils.AccessModeToInt(permissions);
            }          

            await _cmArchieveLinkClient.PutArchiveCertificate(authId, protectionLevel, memoryStream.ToArray(), contRepId);
            return _commandResponseFactory.CreateProtocolText("Certificate published");
        }
        catch (Exception)
        {

            throw;
        }    
    
    }
}
