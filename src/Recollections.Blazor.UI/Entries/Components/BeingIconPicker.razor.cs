using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class BeingIconPicker
    {
        protected List<string> Icons = new List<string>()
        {
            null,
            "user",
            "cat",
            "crow",
            "dog",
            "dove",
            "dragon",
            "feather",
            "feather-alt",
            "fish",
            "frog",
            "hippo",
            "horse",
            "horse-head",
            "kiwi-bird",
            "otter",
            "paw",
            "spider"
        };

        protected Modal Modal { get; set; }

        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public EventCallback<string> Selected { get; set; }

        public void Show() => Modal.Show();
        public void Hide() => Modal.Hide();
    }
}
