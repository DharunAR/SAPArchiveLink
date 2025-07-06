using Microsoft.Extensions.Options;
using System.Text;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public static class TrimServiceInitializer
    {
        private static readonly object _initLock = new();

        public static void InitializeTrimService(IOptionsMonitor<TrimConfigSettings> configMonitor, TrimInitialization initState)
        {
            if (initState.IsInitialized)
                return;

            lock (_initLock)
            {
                if (initState.IsInitialized)
                    return;

                try
                {
                    var trimConfig = configMonitor.CurrentValue;
                    
                    if (string.IsNullOrWhiteSpace(trimConfig.BinariesLoadPath))
                    {
                        TrimApplication.TrimBinariesLoadPath = null;
                    }
                    else
                    {
                        TrimApplication.TrimBinariesLoadPath = trimConfig.BinariesLoadPath;
                    }
                    
                    TrimApplication.Initialize();
                    TrimApplication.SetAsWebService(trimConfig.WorkPath);
                    initState.TrimInitialized();
                }
                catch (TrimException trimEx)
                {
                    TrimApplication.TrimBinariesLoadPath = null;
                    initState.FailInitialization(trimEx.Message);
                }
                catch (Exception ex)
                {
                    initState.FailInitialization(GetFullExceptionMessage(ex));
                }
            }
        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            var sb = new StringBuilder();
            while (ex != null)
            {
                sb.AppendLine(ex.Message);
                ex = ex.InnerException;
            }
            return sb.ToString();
        }
    }

}
