namespace SAPArchiveLink
{
    public class RequestAuthResult
    {
        public bool IsAuthenticated { get; init; }
        public ICommandResponse ErrorResponse { get; init; }

        public static RequestAuthResult Success() => new()
        {
            IsAuthenticated = true,
        };

        public static RequestAuthResult Fail(ICommandResponse error) => new()
        {
            IsAuthenticated = false,
            ErrorResponse = error
        };
    }
}
