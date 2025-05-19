namespace SAPArchiveLink
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<ICommandDispatcherService, ALCommandDispatcher>();

            // Register all ICommandHandler implementations
            services.AddScoped<ICommandHandler, AdmInfoCommandHandler>();
            services.AddScoped<ICommandHandler, AdminContRepCommandHandler>();
            services.AddScoped<ICommandHandler, AnalyzeSecCommandHandler>();
            services.AddScoped<ICommandHandler, AppendCommandHandler>();
            services.AddScoped<ICommandHandler, AttrSearchCommandHandler>();
            services.AddScoped<ICommandHandler, CacheCommandHandler>();
            services.AddScoped<ICommandHandler, CreatePlaceholderCommandHandler>();
            services.AddScoped<ICommandHandler, CreateCommandHandler>();
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
        }
    }
}
