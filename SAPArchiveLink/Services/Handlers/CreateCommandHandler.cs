

namespace SAPArchiveLink
{
    public class CreateCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATE_PUT; // Also handles CREATE_POST

        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            var request = context.GetRequest();

            string contRep = command.GetValue(ALParameter.VarContRep);
            string docId = command.GetValue(ALParameter.VarDocId);
            string pVersion = command.GetValue(ALParameter.VarPVersion);
            string accessMode = command.GetValue(ALParameter.VarAccessMode);
            string compId = command.GetValue(ALParameter.VarCompId);

            if (string.IsNullOrWhiteSpace(contRep) || string.IsNullOrWhiteSpace(docId) || string.IsNullOrWhiteSpace(pVersion))
                return new CommandResponse("Missing required parameters: contRep, docId, pVersion") { StatusCode = 400 };

            bool isMultipart = request.HasFormContentType;

            // Return 403 if doc already exists
            if (await DocumentExists(contRep, docId))
                return new CommandResponse("Document already exists") { StatusCode = 403 };

            if (isMultipart)
            {
                // Handle CREATE_POST: multipart/form-data
                var form = await request.ReadFormAsync();
                var files = form.Files;

                if (files.Count == 0)
                    return new CommandResponse("No components found in multipart body") { StatusCode = 400 };

                foreach (var file in files)
                {
                    string componentId = file.Headers["X-compId"].ToString();
                    if (string.IsNullOrWhiteSpace(componentId))
                        return new CommandResponse("Missing X-compId header in multipart part") { StatusCode = 400 };

                    using var stream = file.OpenReadStream();
                    await SaveComponent(contRep, docId, componentId, stream);
                }

                await SaveMetadata(contRep, docId);
                return new CommandResponse("Document created (POST multipart)") { StatusCode = 201 };
            }
            else
            {
                // Handle CREATE_PUT: single stream
                if (string.IsNullOrWhiteSpace(compId))
                    return new CommandResponse("compId is required for PUT") { StatusCode = 400 };

                if (!request.ContentLength.HasValue || request.ContentLength <= 0)
                    return new CommandResponse("Missing or invalid Content-Length") { StatusCode = 400 };

                using var stream = request.Body;
                await SaveComponent(contRep, docId, compId, stream);
                await SaveMetadata(contRep, docId);
                return new CommandResponse("Document created (PUT)") { StatusCode = 201 };
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
