﻿using Microsoft.AspNetCore.Components;
using Neptuo.Exceptions.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class ExceptionPanel : ComponentBase, IExceptionHandler<Exception>
    {
        public static IReadOnlyCollection<Type> SkippedExceptions { get; } = new[] { typeof(UnauthorizedAccessException) };

        [Inject]
        protected ExceptionHandlerBuilder ExceptionHandlerBuilder { get; set; }

        protected bool IsVisible { get; private set; }
        protected string Title { get; private set; }
        protected string Message { get; private set; }

        protected override Task OnInitializedAsync()
        {
            ExceptionHandlerBuilder.Handler(this);
            return base.OnInitializedAsync();
        }

        void IExceptionHandler<Exception>.Handle(Exception exception)
        {
            IsVisible = true;
            if (IsSkipped(exception))
            {
                IsVisible = false;
                return;
            }

            if (exception is AggregateException aggregateException)
                exception = aggregateException.InnerException;

            if (exception == null)
            {
                Title = "Unspecified weird error";
                Message = "We didn't get any details about problem that occurred. So we can't show you. Apologies for inconvenience.";
            }
            else if (exception is HttpRequestException)
            {
                Title = "Network Error";
                Message = "There was an error during communication with the server. Please reload the page and try again.";
            }
            else
            {
                Title = exception.GetType().Name;
                Message = exception.Message;
            }

            StateHasChanged();
        }

        private static bool IsSkipped(Exception exception)
        {
            Type exceptionType = exception.GetType();
            foreach (Type type in SkippedExceptions)
            {
                if (type.IsAssignableFrom(exceptionType))
                    return true;
            }

            return false;
        }
    }
}
