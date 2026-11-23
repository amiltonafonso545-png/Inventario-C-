using GestorRHPro.Models;
using System.Globalization;

namespace GestorRHPro.Services;

public sealed class AttendanceSchedule
{
    private readonly TimeZoneInfo _timeZone;

    public AttendanceSchedule(IConfiguration configuration)
    {
        StartTime = TimeOnly.ParseExact(configuration["Attendance:StartTime"] ?? "09:00",
            "HH:mm", CultureInfo.InvariantCulture);
        GraceMinutes = configuration.GetValue<int>("Attendance:GraceMinutes");
        if (GraceMinutes is < 0 or >= 1440)
            throw new InvalidOperationException("Attendance:GraceMinutes deve estar entre 0 e 1439.");

        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuration["Attendance:TimeZone"] ?? "Europe/Lisbon");
    }

    public TimeOnly StartTime { get; }
    public int GraceMinutes { get; }

    public DateTime LocalTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _timeZone);

    public DateTime Today => LocalTime(DateTime.UtcNow).Date;

    public Attendance CreateCheckIn(int employeeId, DateTime utc)
    {
        var local = LocalTime(utc);
        var arrivalMinute = local.Hour * 60 + local.Minute;
        var limitMinute = StartTime.Hour * 60 + StartTime.Minute + GraceMinutes;

        return new Attendance
        {
            EmployeeId = employeeId,
            Date = local.Date,
            CheckIn = utc,
            Status = arrivalMinute > limitMinute ? AttendanceStatus.Late : AttendanceStatus.Present
        };
    }
}
