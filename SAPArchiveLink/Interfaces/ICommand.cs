namespace SAPArchiveLink
{
    public interface ICommand
    {
        ALCommandTemplate GetTemplate();

        string GetValue(string key);
        void SetValue(string key, string value);

        string GetURLCharset();
        string GetStringToSign(bool includeSignature, string charset);

        string GetAccessMode();

        bool IsHttpGET();
        bool IsHttpPOST();
        bool IsHttpPUT();
        bool IsHttpDELETE();

        bool IsVerified();
        void SetVerified();

        bool IsImmutable();
        void SetImmutable();

        string GetCertSubject();
        void SetCertSubject(string certSubject);
        bool IsValid { get; }
        string ValidationError { get; }
    }

}
