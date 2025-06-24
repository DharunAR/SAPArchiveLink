namespace SAPArchiveLink
{
    public interface ICommandHandlerRegistry
    {
        ICommandHandler? GetHandler(ALCommandTemplate template);
    }
}
