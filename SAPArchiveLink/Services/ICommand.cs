namespace SAPArchiveLink.Services
{
    public interface ICommand
    {
        ALCommandTemplate Template { get; }
        bool IsHttpGET();
        bool IsHttpPOST();
        bool IsHttpPUT();
        bool IsHttpDELETE();
        string GetValue(string parameterName);
        char GetAccessMode();
    }
}
