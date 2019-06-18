using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries
{
    public class EntryModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime When { get; set; }
        public string Text { get; set; }

        public EntryModel()
        { }

        public EntryModel(string title, DateTime when)
        {
            Title = title;
            When = when;
        }

        public EntryModel(string id, string title, DateTime when, string text)
        {
            Id = id;
            Title = title;
            When = when;
            Text = text;
        }
    }
}
