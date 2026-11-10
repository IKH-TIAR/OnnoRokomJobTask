using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SchoolAdmission.Tests.Infrastructure;

/// <summary>
/// Hosts the real API in memory, pointed at the test database and a controllable clock.
/// </summary>
public sealed class SchoolAdmissionFactory(string connectionString, TimeProvider clock) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("HOLD_DURATION_SECONDS", TestConstants.HoldDurationSeconds.ToString());
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(clock);
        });
    }
}

public static class TestConstants
{
    public const int HoldDurationSeconds = 600;
}
