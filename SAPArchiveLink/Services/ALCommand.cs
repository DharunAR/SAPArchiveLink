﻿namespace SAPArchiveLink
{
    public class ALCommand : ICommand
    {
        private ALCommandTemplate _template;
        private readonly string _httpMethod;
        private readonly CommandParameters _parameters;
        private readonly string _accessMode;
        private readonly string _charset;
        private bool _isImmutable;
        private bool _isVerified;
        private string _certSubject;
        private string _validationError;

        public string ValidationError => _validationError;
        public bool IsValid => string.IsNullOrWhiteSpace(_validationError);

        #region Constants

        public const char PROT_READ = 'r';
        public const char PROT_CREATE = 'c';
        public const char PROT_UPDATE = 'u';
        public const char PROT_DELETE = 'd';
        public const char PROT_ELIB = 'e';
        public const char PROT_NONE = 'n';

        public const int PROT_NO_MAX = 0xFFFF;
        public const int PROT_NO_READ = 1;
        public const int PROT_NO_CREATE = 2;
        public const int PROT_NO_UPDATE = 4;
        public const int PROT_NO_DELETE = 8;
        public const int PROT_NO_ELIB = 16;
        public const int PROT_NO_NONE = 32;

        #endregion

        public ALCommand(CommandRequest context)
        {
            _httpMethod = context.HttpMethod;
            _template = ALCommandTemplateResolver.Parse(_httpMethod, context.Url);
            _charset = "UTF-8";
            _parameters = new CommandParameters();
            _parameters.ParseQueryString(context.Url);
            _accessMode = ALCommandTemplateMetadata.GetAccessMode(_template);

            if (_template == ALCommandTemplate.Unknown)
            {
                _validationError = $"Unsupported command in URL or HTTP method: {context.HttpMethod} {context.Url}";
            }
        }

        public static ICommand FromHttpRequest(CommandRequest context)
        {
            return new ALCommand(context);
        }

        public bool IsHttpGET() => _httpMethod == "GET";
        public bool IsHttpPOST() => _httpMethod == "POST";
        public bool IsHttpPUT() => _httpMethod == "PUT";
        public bool IsHttpDELETE() => _httpMethod == "DELETE";

        public ALCommandTemplate GetTemplate()
        {
            return _template;
        }

        public string GetValue(string key)
        {
            return _parameters.GetValue(key);
        }

        public void SetValue(string key, string value)
        {
            _parameters.SetValue(key, value);
        }

        public string GetURLCharset()
        {
            return _charset;
        }

        public string GetStringToSign(bool includeSignature, string charset)
        {
            return _parameters.GetStringToSign(includeSignature, charset);
        }

        public string GetAccessMode()
        {
            return _accessMode;
        }

        public bool IsVerified()
        {
            return _isVerified;
        }

        public void SetVerified()
        {
            _isVerified = true;
        }

        public bool IsImmutable()
        {
            return _isImmutable;
        }

        public void SetImmutable()
        {
            _isImmutable = true;
        }

        public string GetCertSubject()
        {
            return _certSubject;
        }

        public void SetCertSubject(string certSubject)
        {
            _certSubject = certSubject;
        }
    }
}
