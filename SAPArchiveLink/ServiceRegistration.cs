using Microsoft.Extensions.Options;
using SAPArchiveLink.Services;
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
            services.AddScoped<ICommandHandlerRegistry, CommandHandlerRegistry>();
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
            services.AddScoped<ICertificateFactory, CertificateFactory>();
            RegisterTextExtractors();
        }

        private static void RegisterTextExtractors()
        {
            TextExtractorFactory.Register("text/plain", new PlainTextExtractor());
            TextExtractorFactory.Register("application/pdf", new PdfTextExtractor());
            TextExtractorFactory.Register("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new ExcelTextExtractor());
            TextExtractorFactory.Register("application/vnd.openxmlformats-officedocument.wordprocessingml.document", new DocxTextExtractor());           
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
            services.Configure<TrimConfigSettings>(configSection);

            // Register shared state and services
            services.AddSingleton<TrimInitialization>();
            services.AddSingleton<ISdkMessageProvider, SdkMessageProvider>();
            services.AddScoped<IDownloadFileHandler, DownloadFileHandler>();
        }
    }
}
