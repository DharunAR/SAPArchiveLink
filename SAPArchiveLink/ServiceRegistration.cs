using Microsoft.Extensions.Options;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public static class ServiceRegistration
    {
        /// <summary>
        /// Register required services
        /// </summary>
        /// <param name="services"></param>
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<ICommandDispatcherService, ALCommandDispatcher>();
            services.AddScoped<IVerifier, Verifier>();
            services.AddScoped<ContentServerRequestAuthenticator>();

            // Register all ICommandHandler implementations
            services.AddScoped<ICommandHandler, AdmInfoCommandHandler>();
            services.AddScoped<ICommandHandler, AdminContRepCommandHandler>();
            services.AddScoped<ICommandHandler, AnalyzeSecCommandHandler>();
            services.AddScoped<ICommandHandler, AppendCommandHandler>();
            services.AddScoped<ICommandHandler, AttrSearchCommandHandler>();
            services.AddScoped<ICommandHandler, CacheCommandHandler>();
            services.AddScoped<ICommandHandler, CreatePlaceholderCommandHandler>();
            services.AddScoped<ICommandHandler, CreatePutCommandHandler>();
            services.AddScoped<ICommandHandler, CreatePostCommandHandler>();
            services.AddScoped<ICommandHandler, CsInfoCommandHandler>();
            services.AddScoped<ICommandHandler, CsrvInfoCommandHandler>();
            services.AddScoped<ICommandHandler, CsrvSettingsCommandHandler>();
            services.AddScoped<ICommandHandler, DelAttributeCommandHandler>();
            services.AddScoped<ICommandHandler, DeleteCommandHandler>();
            services.AddScoped<ICommandHandler, DocGetCommandHandler>();
            services.AddScoped<ICommandHandler, DocGetFromCsCommandHandler>();
            services.AddScoped<ICommandHandler, FlushCommandHandler>();
            services.AddScoped<ICommandHandler, FreeSearchCommandHandler>();
            services.AddScoped<ICommandHandler, GetCommandHandler>();
            services.AddScoped<ICommandHandler, GetHeadCommandHandler>();
            services.AddScoped<ICommandHandler, GetAttributeCommandHandler>();
            services.AddScoped<ICommandHandler, GetAtsCommandHandler>();
            services.AddScoped<ICommandHandler, GetCertCommandHandler>();
            services.AddScoped<ICommandHandler, GetDocHistoryCommandHandler>();
            services.AddScoped<ICommandHandler, InfoCommandHandler>();
            services.AddScoped<ICommandHandler, LockCommandHandler>();
            services.AddScoped<ICommandHandler, MCreateCommandHandler>();
            services.AddScoped<ICommandHandler, MigrateCommandHandler>();
            services.AddScoped<ICommandHandler, PutCertCommandHandler>();
            services.AddScoped<ICommandHandler, ReinitCommandHandler>();
            services.AddScoped<ICommandHandler, ReserveDocIdCommandHandler>();
            services.AddScoped<ICommandHandler, SearchCommandHandler>();
            services.AddScoped<ICommandHandler, ServerInfoCommandHandler>();
            services.AddScoped<ICommandHandler, SetAttributeCommandHandler>();
            services.AddScoped<ICommandHandler, SetDocFlagCommandHandler>();
            services.AddScoped<ICommandHandler, SetRecordCommandHandler>();
            services.AddScoped<ICommandHandler, SignUrlCommandHandler>();
            services.AddScoped<ICommandHandler, UnlockCommandHandler>();
            services.AddScoped<ICommandHandler, UpdateCommandHandler>();
            services.AddScoped<ICommandHandler, UpdatePostCommandHandler>();
            services.AddScoped<ICommandHandler, ValidUserCommandHandler>();
            services.AddScoped<ICommandHandler, VerifyAtsCommandHandler>();
            services.AddScoped<ICommandHandler, VerifySigCommandHandler>();
            //services.AddScoped<ICommandHandler, AppendNoteCommandHandler>();
            //services.AddScoped<ICommandHandler, GetNotesCommandHandler>();
            services.AddScoped<ICommandHandler, StoreAnnotationsCommandHandler>();
            services.AddScoped<ICommandHandler, GetAnnotationsCommandHandler>();
            services.AddScoped<ICommandHandler, DistributeContentCommandHandler>();
            services.AddScoped<ICommandHandler, GetContentCommandHandler>();


            services.AddScoped<IDatabaseConnection, DatabaseConnection>();         
            services.AddSingleton<ICommandResponseFactory, CommandResponseFactory>();
            services.AddScoped<IBaseServices, BaseServices>();
            services.AddTransient(typeof(ILogHelper<>), typeof(LogHelper<>));
        }

        /// <summary>
        /// Register Trim configuration values
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void RegisterTrimConfig(IServiceCollection services, IConfiguration configuration)
        {
            var configSection = configuration.GetSection("TRIMConfig");
            var config = configSection.Get<TrimConfigSettings>();
            if (config == null || string.IsNullOrWhiteSpace(config.WorkPath))
            {
                throw new InvalidOperationException("TRIMConfig section is missing, invalid, or WorkPath is not set.");
            }
            services.Configure<TrimConfigSettings>(configSection);
            services.AddSingleton<TrimInitialization>();
            services.AddSingleton<ISdkMessageProvider, SdkMessageProvider>();
            services.AddScoped<IDownloadFileHandler>(sp => new DownloadFileHandler(config.WorkPath));
        }

        /// <summary>
        /// Initialize Trim Application services
        /// </summary>
        /// <param name="trimConfigOptions"></param>
        /// <param name="initState"></param>
        public static void InitializeTrimService(IOptions<TrimConfigSettings> trimConfigOptions, TrimInitialization initState)
        {
            try
            {
                var trimConfig = trimConfigOptions.Value;
                if (!string.IsNullOrWhiteSpace(trimConfig.BinariesLoadPath))
                {
                    TrimApplication.TrimBinariesLoadPath = trimConfig.BinariesLoadPath;
                }
                TrimApplication.SetAsWebService(trimConfig.WorkPath);
                TrimApplication.Initialize();
                initState.TrimInitialized();
            }
            catch (TrimException trimEx)
            {
                initState.FailInitialization(trimEx.Message);
            }
            catch (Exception ex)
            {
                initState.FailInitialization(GetFullExceptionMessage(ex));
            }
        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var current = ex;
            while (current != null)
            {
                messages.Add(current.Message);
                current = current.InnerException;
            }
            return string.Join(", ", messages);
        }
    }
}
