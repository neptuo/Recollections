using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class Modal
    {
        [Inject]
        internal ModalInterop Interop { get; set; }

        [Inject]
        internal ILog<Modal> Log { get; set; }

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string CssClass { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Buttons { get; set; }

        [Parameter]
        public string CloseButtonText { get; set; } = "Close";

        [Parameter]
        public Action FormSubmit { get; set; }

        [Parameter]
        public Action CloseButtonClick { get; set; }

        protected string DialogCssClass { get; set; }

        [Parameter]
        public ModalSize Size { get; set; } = ModalSize.Normal;

        [Parameter]
        public Action Closed { get; set; }

        protected ElementReference Container { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            DialogCssClass = "modal-dialog";
            switch (Size)
            {
                case ModalSize.Small:
                    DialogCssClass += " modal-sm";
                    break;
                case ModalSize.Normal:
                    break;
                case ModalSize.Large:
                    DialogCssClass += " modal-lg";
                    break;
                default:
                    throw Ensure.Exception.NotSupported(Size.ToString());
            }
        }

        protected void OnFormSubmit(EventArgs e) => FormSubmit?.Invoke();

        protected void OnCloseButtonClick()
        {
            Log.Debug("Modal.OnCloseButtonClick");

            if (CloseButtonClick != null)
                CloseButtonClick();
            else
                Hide();
        }

        public void Show() => Interop.Show(Container);

        public void Hide() => Interop.Hide(Container);
    }
}
