using Neptuo;
using Neptuo.Exceptions.Handlers;
using Neptuo.Recollections.Commons.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ExceptionHandlerExtensions
    {
        public static IServiceCollection AddExceptions(this IServiceCollection services)
        {
            Ensure.NotNull(services, "services");

            ExceptionHandlerBuilder handlerBuilder = new ExceptionHandlerBuilder();
            return services
                .AddSingleton(handlerBuilder)
                .AddSingleton<IExceptionHandler>(handlerBuilder)
                .AddTransient<TaskFaultHandler>();
        }
    }
}
