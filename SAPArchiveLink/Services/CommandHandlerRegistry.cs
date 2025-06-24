namespace SAPArchiveLink
{
    public class CommandHandlerRegistry : ICommandHandlerRegistry
    {
        private readonly Dictionary<ALCommandTemplate, ICommandHandler> _handlers;

        public CommandHandlerRegistry(IEnumerable<ICommandHandler> handlers)
        {
            _handlers = new Dictionary<ALCommandTemplate, ICommandHandler>();
            foreach (var handler in handlers)
            {
                if (_handlers.ContainsKey(handler.CommandTemplate))
                    throw new InvalidOperationException($"Duplicate handler for command: {handler.CommandTemplate}");

                _handlers.Add(handler.CommandTemplate, handler);
            }
        }

        public ICommandHandler? GetHandler(ALCommandTemplate template)
        {
            _handlers.TryGetValue(template, out var handler);
            return handler;
        }
    }

}
