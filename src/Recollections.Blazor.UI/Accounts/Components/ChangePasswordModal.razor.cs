using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Components
{
    public partial class ChangePasswordModal
    {
        [Inject]
        protected Api Api { get; set; }

        protected Modal Modal { get; set; }

        protected List<string> ErrorMessages { get; } = new List<string>();
        protected string Current { get; set; }
        protected string New { get; set; }
        protected string ConfirmNew { get; set; }

        protected bool IsChanged { get; set; }

        public async Task ExecuteAsync()
        {
            if (Validate())
            {
                ChangePasswordResponse response = await Api.ChangePasswordAsync(new ChangePasswordRequest(Current, New));
                if (response.IsSuccess)
                {
                    Current = null;
                    New = null;
                    ConfirmNew = null;

                    IsChanged = true;
                    StateHasChanged();

                    await Task.Delay(2 * 1000);

                    Hide();

                    IsChanged = false;
                    StateHasChanged();
                }
                else
                {
                    ErrorMessages.AddRange(response.ErrorMessages);
                }
            }
        }

        private bool Validate()
        {
            ErrorMessages.Clear();

            if (String.IsNullOrEmpty(Current))
                ErrorMessages.Add("Missing current password.");

            if (String.IsNullOrEmpty(New))
                ErrorMessages.Add("Missing new password.");
            else if (String.IsNullOrEmpty(ConfirmNew))
                ErrorMessages.Add("Missing new password.");

            if (New != ConfirmNew)
                ErrorMessages.Add("New password and its confirmation must match.");

            return ErrorMessages.Count == 0;
        }

        public void Show() => Modal.Show();
        public void Hide() => Modal.Hide();
    }
}
