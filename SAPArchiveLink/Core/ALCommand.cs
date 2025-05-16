using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Models
{
    public class ALCommand : ICommand
    {
        public ALCommandTemplate Template { get; private set; }
        private readonly string _httpMethod;
        private readonly CommandParameters _parameters;
        private readonly char _accessMode;

        private ALCommand(string url, string httpMethod, string charset)
        {
            _httpMethod = httpMethod.ToUpper();
            Template = ALCommandTemplateResolver.Parse(httpMethod, url);
            _parameters = new CommandParameters(url, charset);
            _accessMode = ALCommandTemplateMetadata.GetAccessMode(Template);

            string expectedHttpMethod = ALCommandTemplateMetadata.GetHttpMethod(Template);
            if (_httpMethod != expectedHttpMethod)
            {
                throw new ALException(400, $"Invalid HTTP method {_httpMethod} for command {Template}. Expected {expectedHttpMethod}.");
            }
        }

        public static ICommand FromHttpRequest(CommandRequest request)
        {
            return new ALCommand(request.Url, request.HttpMethod, request.Charset);
        }

        public bool IsHttpGET() => _httpMethod == "GET";
        public bool IsHttpPOST() => _httpMethod == "POST";
        public bool IsHttpPUT() => _httpMethod == "PUT";
        public bool IsHttpDELETE() => _httpMethod == "DELETE";

        public string GetValue(string parameterName)
        {
            return _parameters.GetValue(parameterName);
        }

        public char GetAccessMode()
        {
            return _accessMode;
        }
    }
}
