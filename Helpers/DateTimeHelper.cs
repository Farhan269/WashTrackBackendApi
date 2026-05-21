namespace wsahRecieveDelivary.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime GetBangladeshTime()
        {
            DateTime utcNow = DateTime.UtcNow;

            try
            {
                // Try Windows TimeZone ID
                var bdZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, bdZone);
            }
            catch
            {
                // Try Linux/Docker/Mac TimeZone ID (IANA)
                try
                {
                    var bdZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
                    return TimeZoneInfo.ConvertTimeFromUtc(utcNow, bdZone);
                }
                catch
                {
                    // Fallback (Only if OS is missing timezone data)
                    return utcNow.AddHours(6);
                }
            }
        }


        public static DateTime GetBangladeshTimeFromUtc(DateTime utcDateTime)
        {
            DateTime utcNow = DateTime.UtcNow;
            try
            {
                var bdZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, bdZone);
            }
            catch
            {
                try
                {
                    var bdZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
                    return TimeZoneInfo.ConvertTimeFromUtc(utcNow, bdZone);
                }
                catch
                {
                    return utcDateTime.AddHours(6);
                }
            }
        }
    }
}