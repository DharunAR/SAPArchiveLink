
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows.Input;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class CreatePutCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATEPUT; // Also handles CREATE_POST

        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;
        private IDownloadFileHandler _downloadFileHandler;

        public CreatePutCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService, IDownloadFileHandler fileHandleRequest)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
            _downloadFileHandler = fileHandleRequest;
        }

        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var request = context.GetRequest();
                string docId = command.GetValue(ALParameter.VarDocId);

                // Ensure contentType is not null or empty
                if (string.IsNullOrEmpty(request.ContentType))
                {
                    return _responseFactory.CreateError("Content-Type header is missing or invalid.", StatusCodes.Status400BadRequest);
                }

                List<SapDocumentComponent> sapDocumentComponent = await _downloadFileHandler.HandleRequestAsync(request.ContentType, request.Body, docId);
                var sapDocumentCreateRequest = new CreateSapDocumentModel
                {
                    DocId = docId,
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    CompId = command.GetValue(ALParameter.VarCompId),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    ContentLength = request.ContentLength?.ToString() ?? "0",
                    SecKey = command.GetValue(ALParameter.VarSecKey),
                    AccessMode = command.GetValue(ALParameter.VarAccessMode),
                    AuthId = command.GetValue(ALParameter.VarAuthId),
                    Expiration = command.GetValue(ALParameter.VarExpiration),
                    Stream = request.Body,
                    Charset = request.Headers["charset"].ToString(),
                    Version = request.Headers["version"].ToString(),
                    DocProt = request.Headers["docprot"].ToString(),
                    ContentType = request.ContentType,
                };
                if (sapDocumentComponent != null)
                {
                    sapDocumentComponent.First().CompId = sapDocumentCreateRequest.CompId;
                    sapDocumentComponent.First().Charset = sapDocumentCreateRequest.Charset;
                    sapDocumentCreateRequest.Components = sapDocumentComponent;
                }

                return await _baseService.CreateRecord(sapDocumentCreateRequest);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError($"Internal server error: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
      
    }

}
