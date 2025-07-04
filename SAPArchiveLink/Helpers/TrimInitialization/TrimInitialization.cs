namespace SAPArchiveLink
{
    public class TrimInitialization
    {
        private readonly object _lock = new();
        public bool IsInitialized { get; private set; } = false;
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Set the Trim has been initialized
        /// </summary>
        public void TrimInitialized() 
        {
            lock (_lock)
            {
                IsInitialized = true;
                ErrorMessage = null;
            }
        } 

        /// <summary>
        /// Capture the failure of Trim initialization
        /// </summary>
        /// <param name="errorMessage"></param>
        public void FailInitialization(string errorMessage)
        {
            lock(_lock)
            {
                IsInitialized = false;
                ErrorMessage = errorMessage;
            }
        }
    }
}
