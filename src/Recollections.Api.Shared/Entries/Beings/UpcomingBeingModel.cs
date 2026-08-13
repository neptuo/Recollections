using System;

namespace Neptuo.Recollections.Entries.Beings
{
    public class UpcomingBeingModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public DateTime Date { get; set; }
        public bool IsBirthday { get; set; }
        public int? Age { get; set; }
    }
}
