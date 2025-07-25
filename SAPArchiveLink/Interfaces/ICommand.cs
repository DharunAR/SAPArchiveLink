namespace SAPArchiveLink
{
    public interface ICommand
    {
        ALCommandTemplate GetTemplate();
        string GetValue(string key);
        string GetURLCharset();
        string GetStringToSign(bool includeSignature, string charset);
        string GetAccessMode();
        bool IsHttpGET();
        bool IsHttpPOST();
        bool IsHttpPUT();
        bool IsHttpDELETE();
        bool IsVerified();
        void SetVerified();
        bool IsValid { get; }
        string ValidationError { get; }
    }

}
