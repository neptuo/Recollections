using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineEditModel<T> : ComponentBase
    {
        private T originalValue;

        [Parameter]
        public T Value { get; set; }

        [Parameter]
        public Action<T> ValueChanged { get; set; }

        [Parameter]
        public string PlaceHolder { get; set; }

        protected bool IsEditMode { get; set; }

        protected virtual Task OnEditAsync()
        {
            IsEditMode = true;
            originalValue = Value;
            return Task.CompletedTask;
        }

        protected Task OnSaveValueAsync()
        {
            IsEditMode = false;
            return OnValueChangedAsync();
        }

        protected Task OnResetAsync()
        {
            IsEditMode = false;
            Value = originalValue;
            return Task.CompletedTask;
        }

        protected virtual Task OnValueChangedAsync()
        {
            ValueChanged?.Invoke(Value);
            return Task.CompletedTask;
        }

        protected string GetModeCssClass() 
            => IsEditMode ? String.Empty : "inline-editor-viewmode";
    }
}
