namespace SAPArchiveLink
{
    public interface ICommandHandler
    {
        ALCommandTemplate CommandTemplate { get; }
        Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context);
    }
}
