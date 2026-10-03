using System.Diagnostics;

namespace TL.ResilientCore.Infrastructure.Diagnostics;

public static class Telemetry
{
    public const string ServiceName = "TL.ResilientCore";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}
