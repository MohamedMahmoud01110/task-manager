namespace TaskManager.Application.Common;

public static class DateTimeExtensions
{
    /// Npgsql only accepts Kind=Utc for timestamptz. JSON with an offset arrives as Local,
    /// JSON without an offset arrives as Unspecified (we treat it as UTC).
    public static DateTime? ToUtc(this DateTime? value) => value is null ? null : value.Value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
    };
}