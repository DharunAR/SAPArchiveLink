using SAPArchiveLink.Helpers;

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
            services.AddScoped<ICommandHandler, AppendCommandHandler>();
            services.AddScoped<ICommandHandler, AttrSearchCommandHandler>();
            services.AddScoped<ICommandHandler, CreatePutCommandHandler>();
            services.AddScoped<ICommandHandler, CreatePostCommandHandler>();
            services.AddScoped<ICommandHandler, DeleteCommandHandler>();
            services.AddScoped<ICommandHandler, DocGetCommandHandler>();
            services.AddScoped<ICommandHandler, GetCommandHandler>();
            services.AddScoped<ICommandHandler, InfoCommandHandler>();
            services.AddScoped<ICommandHandler, MCreateCommandHandler>();
            services.AddScoped<ICommandHandler, PutCertCommandHandler>();
            services.AddScoped<ICommandHandler, SearchCommandHandler>();
            services.AddScoped<ICommandHandler, ServerInfoCommandHandler>();
            services.AddScoped<ICommandHandler, UpdateCommandHandler>();
            services.AddScoped<ICommandHandler, UpdatePostCommandHandler>();

            services.AddScoped<IDatabaseConnection, DatabaseConnection>();

            services.AddSingleton<ICommandResponseFactory, CommandResponseFactory>();
            services.AddScoped<IBaseServices, BaseServices>();
            services.AddTransient(typeof(ILogHelper<>), typeof(LogHelper<>));
            services.AddScoped<ICertificateFactory, CertificateFactory>();

            services.AddSingleton<ICounterCache, InMemoryCounterCache>();
            services.AddSingleton<CounterFlusher>();
            services.AddHostedService<CounterFlushHostedService>();
            services.AddSingleton<CounterService>();

            RegisterTextExtractors();
            RegisterContentAppender();
        }

        private static void RegisterTextExtractors()
        {
            TextExtractorFactory.Register("text/plain", new PlainTextExtractor());
            TextExtractorFactory.Register("application/pdf", new PdfTextExtractor());
            TextExtractorFactory.Register("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new ExcelTextExtractor());
            TextExtractorFactory.Register("application/vnd.openxmlformats-officedocument.wordprocessingml.document", new DocxTextExtractor());
            TextExtractorFactory.Register("application/vnd.openxmlformats-officedocument.presentationml.presentation", new PowerPointTextExtractor());
            
        }
        private static void RegisterContentAppender()
        {
            DocumentAppenderFactory.Register(".txt", new TextDocumentAppender());
            DocumentAppenderFactory.Register(".pdf", new PdfDocumentAppender());
            DocumentAppenderFactory.Register(".docx", new WordDocumentAppender());
            DocumentAppenderFactory.Register(".xlxs", new ExcelDocumentAppender());
            DocumentAppenderFactory.Register(".pptx", new PowerPointSlideAppender());
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
