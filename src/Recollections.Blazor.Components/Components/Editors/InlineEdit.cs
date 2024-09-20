using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineEdit<T> : ComponentBase
    {
        private T originalValue;

        [Parameter]
        public T Value { get; set; }

        [Parameter]
        public Action<T> ValueChanged { get; set; }

        [Parameter]
        public string PlaceHolder { get; set; }

        [Parameter]
        public string EditModeCssClass { get; set; }

        [CascadingParameter]
        public FormState FormState { get; set; }

        protected bool IsEditMode { get; set; }
        protected virtual bool IsEditable => FormState?.IsEditable ?? true;

        protected virtual Task OnEditAsync()
        {
            if (IsEditable)
            {
                IsEditMode = true;
                originalValue = Value;
            }

            return Task.CompletedTask;
        }

        protected Task OnSaveValueAsync()
        {
            if (!IsEditable)
                return Task.CompletedTask;

            IsEditMode = false;
            return OnValueChangedAsync();
        }

        protected Task OnResetAsync()
        {
            if (!IsEditable)
                return Task.CompletedTask;

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
        {
            List<string> cssClass = new List<string>();
            if (!IsEditMode)
                cssClass.Add("inline-editor-viewmode");
            else if (!String.IsNullOrEmpty(EditModeCssClass))
                cssClass.Add(EditModeCssClass);

            if (IsEditable)
                cssClass.Add("inline-editable");

            return String.Join(" ", cssClass);
        }
    }
}
