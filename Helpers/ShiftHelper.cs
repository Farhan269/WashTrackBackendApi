using wsahRecieveDelivary.Models;
using wsahRecieveDelivary.Models.Enums;

namespace wsahRecieveDelivary.Helpers
{
    public static class ShiftHelper
    {
        public static (DateOnly ShiftDate, ShiftType ShiftType, int ShiftScheduleId)
    CalculateShift(DateTime transactionDate, ShiftSchedule activeSchedule)
        {
            if (activeSchedule == null)
                throw new ArgumentNullException(nameof(activeSchedule));

            DateTime bdTime = transactionDate; // Already BD time as per your fix
            TimeSpan timeOfDay = bdTime.TimeOfDay;

            //Console.WriteLine($"   [ShiftHelper] Transaction Time: {bdTime:yyyy-MM-dd HH:mm:ss.fff}");
            //Console.WriteLine($"   [ShiftHelper] Time of Day: {timeOfDay}");
            //Console.WriteLine($"   [ShiftHelper] NightShiftStart: {activeSchedule.NightShiftStart}");
            //Console.WriteLine($"   [ShiftHelper] Is >= NightShiftStart: {timeOfDay >= activeSchedule.NightShiftStart}");

            // DAY SHIFT
            if (timeOfDay >= activeSchedule.DayShiftStart &&
                timeOfDay < activeSchedule.DayShiftEnd)
            {
                //Console.WriteLine($"   [ShiftHelper] Branch: Day Shift");
                //Console.WriteLine($"   [ShiftHelper] ShiftDate: {DateOnly.FromDateTime(bdTime):yyyy-MM-dd}");
                return (
                    DateOnly.FromDateTime(bdTime), // Convert DateTime to DateOnly
                    ShiftType.Day,
                    activeSchedule.Id
                );
            }

            // NIGHT SHIFT
            DateOnly shiftDate =
                timeOfDay >= activeSchedule.NightShiftStart
                    ? DateOnly.FromDateTime(bdTime).AddDays(1) // evening part (adds 1 day directly to DateOnly)
                    : DateOnly.FromDateTime(bdTime);           // after midnight part

            //Console.WriteLine($"   [ShiftHelper] Branch: Night Shift ({(timeOfDay >= activeSchedule.NightShiftStart ? "Evening" : "Morning")})");
            //Console.WriteLine($"   [ShiftHelper] ShiftDate: {shiftDate:yyyy-MM-dd}");
            //Console.WriteLine($"   [ShiftHelper] Original Date: {DateOnly.FromDateTime(bdTime):yyyy-MM-dd}");

            return (
                shiftDate,
                ShiftType.Night,
                activeSchedule.Id
            );
        }
        //public static (DateTime ShiftDate, ShiftType ShiftType, int ShiftScheduleId)
        //    CalculateShift(DateTime transactionDate, ShiftSchedule activeSchedule)
        //{
        //    if (activeSchedule == null)
        //        throw new ArgumentNullException(nameof(activeSchedule));

        //    DateTime bdTime = DateTimeHelper.GetBangladeshTimeFromUtc(transactionDate);
        //    TimeSpan timeOfDay = bdTime.TimeOfDay;

        //    DateTime shiftDate;
        //    ShiftType shiftType;

        //    if (timeOfDay >= activeSchedule.DayShiftStart && timeOfDay < activeSchedule.DayShiftEnd)
        //    {
        //        shiftDate = bdTime.Date;
        //        shiftType = ShiftType.Day;
        //    }
        //    else if (timeOfDay >= activeSchedule.NightShiftStart || timeOfDay < activeSchedule.NightShiftEnd)
        //    {
        //        if (timeOfDay < activeSchedule.NightShiftEnd)
        //        {
        //            shiftDate = bdTime.Date.AddDays(-1);
        //        }
        //        else
        //        {
        //            shiftDate = bdTime.Date;
        //        }
        //        shiftType = ShiftType.Night;
        //    }
        //    else
        //    {
        //        shiftDate = bdTime.Date;
        //        shiftType = ShiftType.Night;
        //    }

        //    return (shiftDate, shiftType, activeSchedule.Id);
        //}
    }
}
