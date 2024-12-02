using Neptuo;
using Neptuo.Exceptions.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Exceptions
{
    public class TaskFaultHandler : IExceptionHandler
    {
        private readonly IExceptionHandler exceptionHandler;

        public TaskFaultHandler(IExceptionHandler exceptionHandler)
        {
            Ensure.NotNull(exceptionHandler, "exceptionHandler");
            this.exceptionHandler = exceptionHandler;
        }

        public Task Wrap(Task task) => task.ContinueWith(Handle);
        public Task<T> Wrap<T>(Task<T> task)
        {
            return task.ContinueWith(Handle);
        }

        private void Handle(Task task) => TryProcess(task);
        private T Handle<T>(Task<T> task)
        {
            if (TryProcess(task))
            {
                ExceptionDispatchInfo info = ExceptionDispatchInfo.Capture(task.Exception.InnerException);
                info.Throw();
            }

            return task.Result;
        }

        private bool TryProcess(Task task)
        {
            if (task.IsFaulted)
            {
                Handle(task.Exception);
                return true;
            }

            return false;
        }

        public void Handle(Exception exception) => exceptionHandler.Handle(exception);
    }
}
