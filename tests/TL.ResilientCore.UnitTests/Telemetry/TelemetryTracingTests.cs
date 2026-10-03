using System.Diagnostics;
using FluentAssertions;
using Xunit;
using AppTelemetry = TL.ResilientCore.Infrastructure.Diagnostics.Telemetry;

namespace TL.ResilientCore.UnitTests.Telemetry;

public class TelemetryTracingTests
{
    [Fact]
    public void ActivitySource_DevePossuirNomeDoServicoConfigurado()
    {
        AppTelemetry.ServiceName.Should().Be("TL.ResilientCore");
        AppTelemetry.ActivitySource.Name.Should().Be("TL.ResilientCore");
    }

    [Fact]
    public void ActivitySource_QuandoOuvido_DeveEmitirTraceIdValido()
    {
        var activityCaptured = false;
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == AppTelemetry.ServiceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                activityCaptured = true;
                activity.TraceId.Should().NotBe(default(ActivityTraceId));
            }
        };

        ActivitySource.AddActivityListener(listener);

        using (var activity = AppTelemetry.ActivitySource.StartActivity("TesteEnvioEvento", ActivityKind.Producer))
        {
            activity.Should().NotBeNull();
            activity!.SetTag("messaging.system", "rabbitmq");
        }

        activityCaptured.Should().BeTrue();
    }
}
