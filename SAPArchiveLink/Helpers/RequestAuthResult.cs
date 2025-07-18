namespace SAPArchiveLink
{
    public class RequestAuthResult
    {
        public bool IsAuthenticated { get; init; }
        public ICommandResponse ErrorResponse { get; init; }

        /// <summary>
        /// Creates a successful authentication result.
        /// </summary>
        /// <returns></returns>
        public static RequestAuthResult Success() => new()
        {
            IsAuthenticated = true,
        };

        /// <summary>
        ///  Creates a failed authentication result with an error response.
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static RequestAuthResult Fail(ICommandResponse error) => new()
        {
            IsAuthenticated = false,
            ErrorResponse = error
        };
    }
}
