using System;

namespace Neptuo.Recollections
{
    public static class BirthDateUtils
    {
        /// <summary>
        /// Returns the age in whole years reached at <paramref name="at"/> for someone
        /// born on <paramref name="birthDate"/>. Negative results are clamped to zero.
        /// </summary>
        public static int GetAge(DateTime birthDate, DateTime at)
        {
            int age = at.Year - birthDate.Year;
            if (at.Month < birthDate.Month || (at.Month == birthDate.Month && at.Day < birthDate.Day))
                age--;

            return age < 0 ? 0 : age;
        }
    }
}
