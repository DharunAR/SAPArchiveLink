
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows.Input;

namespace SAPArchiveLink
{
    public class CreatePutCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATEPUT; // Also handles CREATE_POST

        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;

        public CreatePutCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
        }

        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var request = context.GetRequest();

                var sapDocumentCreateRequest = new CreateSapDocumentModel
                {
                    DocId = command.GetValue(ALParameter.VarDocId),
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    CompId = command.GetValue(ALParameter.VarCompId),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    ContentLength = request.ContentLength?.ToString() ?? "0",
                    SecKey = command.GetValue(ALParameter.VarSecKey),
                    AccessMode = command.GetValue(ALParameter.VarAccessMode),
                    AuthId = command.GetValue(ALParameter.VarAuthId),
                    Expiration = command.GetValue(ALParameter.VarExpiration),
                    Stream = context.GetInputStream(),
                };


                return await _baseService.CreateRecord(sapDocumentCreateRequest);
            }
            catch (Exception ex)
            {

                return _responseFactory.CreateError($"Internal server error: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        private Task<bool> DocumentExists(string contRep, string docId)
        {
            string path = Path.Combine("Repo", contRep, docId);
            return Task.FromResult(Directory.Exists(path));
        }

        private async Task SaveComponent(string contRep, string docId, string compId, Stream stream)
        {
            string dir = Path.Combine("Repo", contRep, docId);
            Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, compId);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fs);
        }

        private Task SaveMetadata(string contRep, string docId)
        {
            // Save timestamps or other document metadata here
            return Task.CompletedTask;
        }
    }

}
