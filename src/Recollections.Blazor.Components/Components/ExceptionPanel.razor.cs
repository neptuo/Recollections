using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Neptuo.Exceptions.Handlers;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class ExceptionPanel : ComponentBase, IExceptionHandler<Exception>, IDisposable
    {
        public static IReadOnlyCollection<Type> SkippedExceptions { get; } = new[] { typeof(UnauthorizedAccessException) };

        [Inject]
        protected ExceptionHandlerBuilder ExceptionHandlerBuilder { get; set; }

        [Inject]
        internal FreeLimitsNotifier FreeLimitsNotifier { get; set; }

        [Inject]
        protected ILog<ExceptionPanel> Log { get; set; }

        [Inject]
        protected WindowInterop WindowInterop { get; set; }

        [Parameter]
        public RenderFragment NotFoundContent { get; set; }

        protected bool IsVisible { get; private set; }
        protected string Title { get; private set; }
        protected string Message { get; private set; }
        protected bool IsNotFound { get; private set; }

        protected ElementReference Container { get; private set; }
        protected Modal PremiumModal { get; private set; }

        protected override Task OnInitializedAsync()
        {
            ExceptionHandlerBuilder.Handler(this);
            FreeLimitsNotifier.OnShow += OnFreeLimitsNotifierShow;
            return base.OnInitializedAsync();
        }

        public void Dispose()
        {
            FreeLimitsNotifier.OnShow -= OnFreeLimitsNotifierShow;
        }

        private void OnFreeLimitsNotifierShow()
            => PremiumModal.Show();

        void IExceptionHandler<Exception>.Handle(Exception exception)
        {
            IsNotFound = false;
            IsVisible = true;
            if (IsSkipped(exception))
            {
                IsVisible = false;
                return;
            }

            if (exception is AggregateException aggregateException)
                exception = aggregateException.InnerException;

            Log.Debug(exception.ToString());

            if (exception == null)
            {
                Title = "Unspecified weird error";
                Message = "We didn't get any details about the problem that occurred. So we can't show you. Apologies for inconvenience.";
            }
            else if (exception.Message == "TypeError: Failed to fetch")
            {
                SetNetworkErrorMessage();
            }
            else if (exception is HttpRequestException httpException)
            {
                if (IsHttpResponseStatusCode(httpException, 401))
                {
                    IsNotFound = true;
                }
                if (IsHttpResponseStatusCode(httpException, 503))
                {
                    Title = "Server Update";
                    Message = "The server part of the application is currently being updated. Please come back later.";
                }
                else if (IsHttpResponseStatusCode(httpException, 404))
                {
                    Title = "Not found";
                    Message = "Requested resource was not found on the server.";
                }
                else if (IsHttpResponseStatusCode(httpException, 402))
                {
                    PremiumModal.Show();
                    IsVisible = false;
                }
                else
                {
                    SetNetworkErrorMessage();
                }
            }
            else if (exception is UnauthorizedAccessException)
            {
                IsNotFound = true;
            }
            else if (exception is FreeLimitsReachedExceptionException)
            {
                PremiumModal.Show();
                IsVisible = false;
            }
            else
            {
                Title = exception.GetType().Name;
                Message = exception.Message;
            }

            StateHasChanged();

            if (IsVisible)
                WindowInterop.ScrollTo(0, 0);
        }

        private void SetNetworkErrorMessage()
        {
            Title = "Network Error";
            Message = "There was an error during communication with the server. Please reload the page and try again.";
        }

        private static bool IsHttpResponseStatusCode(HttpRequestException exception, int statusCode)
            => exception.StatusCode == (HttpStatusCode)statusCode || exception.Message.Contains($"Response status code does not indicate success: {statusCode}");

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
