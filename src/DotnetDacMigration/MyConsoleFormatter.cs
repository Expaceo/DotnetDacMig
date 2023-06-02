using Crayon;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crayon;

namespace DotnetDacMigration
{
    internal sealed class MyConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private const string LoglevelPadding = " : ";
        private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
        private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        private IDisposable _optionsReloadToken;

        public MyConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
            : base("myConsoleFormatter")
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal SimpleConsoleFormatterOptions FormatterOptions { get; set; }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.State is IReadOnlyCollection<KeyValuePair<string, object>> stateProperties)
            {
                message = stateProperties.First(kvp => kvp.Key == "{OriginalFormat}").Value.ToString();
                foreach (KeyValuePair<string, object> item in stateProperties)
                {
                    message = message.Replace("{" + item.Key + "}", Crayon.Output.Blue(item.Value.ToString()));
                }
            }
            else
            {
                message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            }
            if (logEntry.Exception == null && message == null)
            {
                return;
            }
            LogLevel logLevel = logEntry.LogLevel;
            string logLevelString = GetLogLevelString(logLevel);

            if (logLevelString != null)
            {
                WriteColoredMessage(textWriter, logLevel, logLevelString);
            }
            CreateDefaultLogMessage(textWriter, logEntry, message);
        }

        private void WriteColoredMessage(TextWriter textWriter, LogLevel logLevel, string logLevelString)
        {

            string msg = logLevel switch
            {
                LogLevel.Information => Crayon.Output.Green(logLevelString),
                LogLevel.Error => Crayon.Output.Red().Text(logLevelString),
                LogLevel.Warning => Crayon.Output.Yellow().Text(logLevelString),
                _ => logLevelString
            };;
           
            textWriter.Write(msg);
        }

        private void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message)
        {
            bool singleLine = FormatterOptions.SingleLine;
            Exception exception = logEntry.Exception;

            textWriter.Write(LoglevelPadding);
            WriteMessage(textWriter, message, singleLine);

            if (exception != null)
            {
                // exception message
                WriteMessage(textWriter, exception.ToString(), singleLine);
            }
                
            textWriter.Write(Environment.NewLine);
        }

        private void WriteMessage(TextWriter textWriter, string message, bool singleLine)
        {
            if (!string.IsNullOrEmpty(message))
            {
                WriteReplacing(textWriter, Environment.NewLine, _newLineWithMessagePadding, message);
            }

            static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
            {
                string newMessage = message.Replace(oldValue, newValue);
                writer.Write(newMessage);
            }
        }

        private DateTimeOffset GetCurrentDateTime()
        {
            return FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "[trce]",
                LogLevel.Debug => "[dbug]",
                LogLevel.Information => "[info]",
                LogLevel.Warning => "[warn]",
                LogLevel.Error => "[fail]",
                LogLevel.Critical => "[crit]",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }
}
