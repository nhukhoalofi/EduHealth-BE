namespace EduHealth.Helpers
{
    public static class VietnamTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

        public static DateOnly TodayDateOnly => DateOnly.FromDateTime(Now);

        public static DateTime ToVietnamTime(DateTime dateTime)
        {
            if (dateTime == default)
            {
                return dateTime;
            }

            return dateTime.Kind switch
            {
                DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dateTime, VietnamTimeZone),
                DateTimeKind.Local => TimeZoneInfo.ConvertTime(dateTime, VietnamTimeZone),
                _ => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), VietnamTimeZone)
            };
        }

        public static DateTime? ToVietnamTime(DateTime? dateTime)
        {
            return dateTime.HasValue ? ToVietnamTime(dateTime.Value) : null;
        }

        private static TimeZoneInfo ResolveVietnamTimeZone()
        {
            foreach (var timeZoneId in new[] { "SE Asia Standard Time", "Asia/Bangkok", "Asia/Ho_Chi_Minh" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.CreateCustomTimeZone(
                id: "Vietnam Standard Time",
                baseUtcOffset: TimeSpan.FromHours(7),
                displayName: "(UTC+07:00) Vietnam",
                standardDisplayName: "Vietnam Standard Time");
        }
    }
}
