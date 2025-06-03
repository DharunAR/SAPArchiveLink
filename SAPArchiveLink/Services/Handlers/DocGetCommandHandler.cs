

using System.Text;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class DocGetCommandHandler : ICommandHandler
    {
        private ICMArchieveLinkClient _archiveClient;
        public DocGetCommandHandler(ICMArchieveLinkClient archiveClient)
        {
            _archiveClient = archiveClient;
        }

        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DOCGET;

        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                string docId = command.GetValue(ALParameter.VarDocId);
                string contRep = command.GetValue(ALParameter.VarContRep);
                string compId = command.GetValue(ALParameter.VarCompId);
                string pVersion = command.GetValue(ALParameter.VarPVersion);

                if (string.IsNullOrEmpty(docId) || string.IsNullOrEmpty(contRep))
                    return CommandResponse.ForError("Missing required parameters: docId and contRep", "ICS_4001");
                
                // Dispose the database context in the handler to avoid premature disposal.
                // If disposed inside ArchiveClient, data access in the handler will fail.
                using (var db = _archiveClient.GetDatabase())
                {
                    var record = _archiveClient.GetRecord(db, docId, contRep);
                    if (record == null)
                        return CommandResponse.ForError("Record not found", "ICS_4040", StatusCodes.Status404NotFound);

                    var components = record.ChildSapComponents;
                    //if (components == null || components.Count == 0)
                    //    return CommandResponse.ForError("No components found for the document", "ICS_4041", StatusCodes.Status404NotFound);

                    if (!string.IsNullOrEmpty(compId))
                    {
                        if (!_archiveClient.IsRecordComponentAvailable(components, compId))
                        {
                            return CommandResponse.ForError($"Component '{compId}' not found", "ICS_4042", StatusCodes.Status404NotFound);
                        }
                    }

                    var documentComponents = _archiveClient.GetDocumentComponents(components);

                    return CommandResponse.ForMultipartDocument(documentComponents);
                }    
            }
            catch (Exception ex)
            {
                return null;
            } 
        }


    }
}
