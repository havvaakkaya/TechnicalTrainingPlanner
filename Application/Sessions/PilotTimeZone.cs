namespace TechnicalTrainingPlanner.Application.Sessions;

public static class PilotTimeZone
{
    private static readonly TimeZoneInfo Zone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);

    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    public static bool IsInvalid(DateTime local) =>
        Zone.IsInvalidTime(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));

    public static bool IsAmbiguous(DateTime local) =>
        Zone.IsAmbiguousTime(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));
}
