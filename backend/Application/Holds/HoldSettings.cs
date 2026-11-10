namespace SchoolAdmission.Application.Holds;

/// <summary>How long a seat stays reserved. Set via the HOLD_DURATION_SECONDS env var (default 600).</summary>
public record HoldSettings(TimeSpan Duration)
{
    public const string DurationEnvVar = "HOLD_DURATION_SECONDS";
    public const int DefaultDurationSeconds = 600;
}
