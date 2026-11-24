namespace ConsultasPlan.Helpers;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime fecha)
    {
        return fecha.Kind switch
        {
            DateTimeKind.Utc => fecha,
            DateTimeKind.Local => fecha.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified),
                TimeZoneInfo.Local)
        };
    }

    public static bool IsInCurrentYear(this DateTime fecha)
    {
        return fecha.ToUtc().Year == DateTime.UtcNow.Year;
    }
}
