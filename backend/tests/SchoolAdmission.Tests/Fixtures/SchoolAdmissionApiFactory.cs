using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SchoolAdmission.Tests.Fixtures;

public class SchoolAdmissionApiFactory : WebApplicationFactory<Program>
{
    protected static readonly string ResolvedConnectionString = ResolveConnectionString();

    protected virtual string HoldDurationSeconds => "600";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ResolvedConnectionString,
                ["HOLD_DURATION_SECONDS"] = HoldDurationSeconds,
                ["SEED_DEMO_DATA"] = "false"
            });
        });
    }

    private static string ResolveConnectionString()
    {
        var envConn = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(envConn))
        {
            return WithTimeouts(envConn);
        }

        // Try Docker container IP on host if reachable
        foreach (var ip in new[] { "172.18.0.2", "172.17.0.2", "172.19.0.2", "172.20.0.2" })
        {
            if (CanConnectToPort(ip, 5432))
            {
                return WithTimeouts($"Host={ip};Port=5432;Database=school_admission;Username=postgres;Password=postgres;Include Error Detail=true;");
            }
        }

        // Fallback to localhost:5432
        return WithTimeouts("Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres;Include Error Detail=true;");
    }

    private static string WithTimeouts(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = 60,
            Timeout = 60,
            CommandTimeout = 60
        };
        return builder.ConnectionString;
    }

    private static bool CanConnectToPort(string host, int port)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectResult = tcpClient.BeginConnect(host, port, null, null);
            var completed = connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
            if (!completed)
            {
                return false;
            }
            tcpClient.EndConnect(connectResult);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class ExpiryTestApiFactory : SchoolAdmissionApiFactory
{
    protected override string HoldDurationSeconds => "2";
}
