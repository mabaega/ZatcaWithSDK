using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;

namespace ZatcaWithSDK
{
    public class CustomConsoleFormatter : ConsoleFormatter
    {
        public CustomConsoleFormatter() : base("custom") { }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            if (logEntry.LogLevel == LogLevel.Information)
            {
                textWriter.WriteLine(logEntry.State);
            }
        }
    }
}
