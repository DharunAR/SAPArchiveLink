﻿using SAPArchiveLink.Resources;
namespace SAPArchiveLink
{
    public class MCreateCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.MCREATE;
        private readonly ICommandResponseFactory _responseFactory;
        private readonly IBaseServices _baseService;
        private readonly IDownloadFileHandler _downloadFileHandler;

        public MCreateCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService, IDownloadFileHandler fileHandleRequest)
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
                var allComponents = await _downloadFileHandler.HandleRequestAsync(request.ContentType, request.Body, null);
                var groupedByDocId = allComponents.GroupBy(c => c.DocId);

                var results = new List<string>();

                foreach (var group in groupedByDocId)
                {
                    var docId = group.Key;
                    var model = new CreateSapDocumentModel
                    {
                        DocId = docId,
                        ContRep = command.GetValue(ALParameter.VarContRep),
                        PVersion = command.GetValue(ALParameter.VarPVersion),
                        AccessMode = command.GetValue(ALParameter.VarAccessMode),
                        AuthId = command.GetValue(ALParameter.VarAuthId),
                        Expiration = command.GetValue(ALParameter.VarExpiration),
                        SecKey = command.GetValue(ALParameter.VarSecKey),
                        Charset = request.Headers["charset"].ToString(),
                        Version = request.Headers["version"].ToString(),
                        DocProt = command.GetValue(ALParameter.VarDocProt),
                        Components = group.ToList(),
                        ContentType = request.ContentType,
                        ContentLength = request.ContentLength?.ToString() ?? "0"
                    };

                    var response = await _baseService.CreateRecord(model, true);
                    results.Add(FormatResultLine(docId, response));
                }
                var finalResponse = string.Join("", results);
                return _responseFactory.CreateProtocolText(finalResponse, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError(string.Format(Resource.Error_InternalServer, ex.Message), StatusCodes.Status500InternalServerError);
            }
        }

        private string FormatResultLine(string docId, ICommandResponse response)
        {
            string errorDes = string.Empty;
            var code = response.StatusCode;
            if (response.Headers != null && response.Headers.TryGetValue("X-ErrorDescription", out string value))
            {
                errorDes = value;
            }
            return $"docId=\"{docId}\";retCode=\"{code}\";errorDescription=\"{errorDes}\"\r\n";
        }
    }
}
