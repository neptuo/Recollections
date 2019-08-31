using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class ModalModel : ComponentBase
    {
        [Inject]
        internal ModalInterop Interop { get; set; }

        [Parameter]
        protected string Title { get; set; }

        [Parameter]
        protected RenderFragment ChildContent { get; set; }

        [Parameter]
        protected RenderFragment Buttons { get; set; }

        [Parameter]
        protected string CloseButtonText { get; set; } = "Close";

        [Parameter]
        protected Action FormSubmit { get; set; }

        [Parameter]
        protected Action CloseButtonClick { get; set; }

        protected string DialogCssClass { get; set; }

        [Parameter]
        protected ModalSize Size { get; set; } = ModalSize.Normal;

        [Parameter]
        protected Action Closed { get; set; }

        protected ElementRef Container { get; set; }

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

        protected void OnFormSubmit(UIEventArgs e) => FormSubmit?.Invoke();

        protected void OnCloseButtonClick()
        {
            if (CloseButtonClick != null)
                CloseButtonClick();
            else
                Hide();
        }

        public void Show() => Interop.Show(Container);

        public void Hide() => Interop.Hide(Container);
    }
}
