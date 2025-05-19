



namespace SAPArchiveLink
{
    public interface ICommandHandler
    {
        ALCommandTemplate CommandTemplate { get; }
        Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context);
    }
}
