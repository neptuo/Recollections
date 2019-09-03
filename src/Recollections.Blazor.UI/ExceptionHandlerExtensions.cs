using Microsoft.Extensions.DependencyInjection;
using Neptuo;
using Neptuo.Exceptions.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
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
