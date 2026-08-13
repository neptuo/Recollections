using System;
using System.Collections.Generic;
using Neptuo.Recollections.Entries.Beings;

namespace Neptuo.Recollections.Entries
{
    public static class UpcomingBeingUtils
    {
        public static List<UpcomingBeingModel> Create(
            IEnumerable<Being> beings,
            DateTime today,
            bool includeBirthdays,
            bool includeNameDays)
        {
            var result = new List<UpcomingBeingModel>();
            foreach (Being being in beings)
            {
                if (includeBirthdays && being.BirthDate.HasValue)
                {
                    DateTime date = GetNextDate(being.BirthDate.Value.Month, being.BirthDate.Value.Day, today);
                    result.Add(new UpcomingBeingModel
                    {
                        Id = being.Id,
                        Name = being.Name,
                        Icon = being.Icon,
                        Date = date,
                        IsBirthday = true,
                        Age = GetBirthdayAge(being.BirthDate.Value, date)
                    });
                }

                if (includeNameDays && being.NameDayMonth.HasValue && being.NameDayDay.HasValue)
                {
                    result.Add(new UpcomingBeingModel
                    {
                        Id = being.Id,
                        Name = being.Name,
                        Icon = being.Icon,
                        Date = GetNextDate(being.NameDayMonth.Value, being.NameDayDay.Value, today),
                        IsBirthday = false
                    });
                }
            }

            result.Sort((x, y) =>
            {
                int dateResult = x.Date.CompareTo(y.Date);
                return dateResult != 0 ? dateResult : StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
            });
            return result;
        }

        public static DateTime GetNextDate(int month, int day, DateTime today)
        {
            int year = today.Year;
            int actualDay = Math.Min(day, DateTime.DaysInMonth(year, month));
            DateTime date = new DateTime(year, month, actualDay);
            if (date < today.Date)
            {
                year++;
                actualDay = Math.Min(day, DateTime.DaysInMonth(year, month));
                date = new DateTime(year, month, actualDay);
            }

            return date;
        }

        private static int GetBirthdayAge(DateTime birthDate, DateTime birthday)
        {
            int age = BirthDateUtils.GetAge(birthDate, birthday);
            if (birthDate.Month == 2 && birthDate.Day == 29
                && birthday.Month == 2 && birthday.Day == 28
                && !DateTime.IsLeapYear(birthday.Year))
            {
                age++;
            }

            return age;
        }
    }
}
