namespace SAPArchiveLink
{
    public interface ILogHelper<T>
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
    }
}
