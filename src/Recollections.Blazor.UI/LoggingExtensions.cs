using Microsoft.Extensions.DependencyInjection;
using Neptuo;
using Neptuo.Logging;
using Neptuo.Logging.Serialization;
using Neptuo.Logging.Serialization.Filters;
using Neptuo.Logging.Serialization.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public static class LoggingExtensions
    {
        public static IServiceCollection AddLogging(this IServiceCollection services)
        {
            Ensure.NotNull(services, "services");

            ILogFilter logFilter = DefaultLogFilter.Debug;

#if !DEBUG
            logFilter = DefaultLogFilter.Warning;
#endif

            ILogFactory logFactory = new DefaultLogFactory("Root");
            logFactory.AddSerializer(new ConsoleSerializer(new SingleLineLogFormatter(), logFilter));

            return services
                .AddSingleton(logFactory)
                .AddTransient(typeof(ILog<>), typeof(DefaultLog<>));
        }
    }
}

namespace Neptuo.Logging
{
    internal class DefaultLog<T> : ILog<T>
    {
        private ILog log;

        public DefaultLog(ILogFactory logFactory)
        {
            Ensure.NotNull(logFactory, "logFactory");
            log = logFactory.Scope(typeof(T).Name);
        }

        public ILogFactory Factory => log.Factory;

        public bool IsLevelEnabled(LogLevel level)
            => log.IsLevelEnabled(level);

        public void Log(LogLevel level, object model)
            => log.Log(level, model);
    }
}

namespace Neptuo.Logging.Serialization.Formatters
{
    public class SingleLineLogFormatter : ILogFormatter
    {
        public string Format(string scopeName, LogLevel level, object model)
        {
            object message;
            if (!Converts.Try(model.GetType(), typeof(string), model, out message))
                message = model;

            return String.Format(
                "{0} {1}({2}): {3}",
                DateTime.Now.ToShortTimeString(),
                scopeName,
                level.ToString().ToUpperInvariant(),
                message
            );
        }
    }
}