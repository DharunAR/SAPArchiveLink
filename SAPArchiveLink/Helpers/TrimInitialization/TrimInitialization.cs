namespace SAPArchiveLink
{
    public class TrimInitialization
    {
        public bool IsInitialized { get; private set; }
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Set the Trim has been initialized
        /// </summary>
        public void TrimInitialized() => IsInitialized = true;

        /// <summary>
        /// Capture the failure of Trim initialization
        /// </summary>
        /// <param name="errorMessage"></param>
        public void FailInitialization(string errorMessage)
        {
            IsInitialized = false;
            ErrorMessage = errorMessage;
        }
    }
}
