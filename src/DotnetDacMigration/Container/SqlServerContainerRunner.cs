using Microsoft.Data.SqlClient;

namespace DotnetDacMigration.Container;

/// <summary>
/// Starts a new SQL Server 2022 docker container.
/// </summary>
public sealed class SqlServerContainerRunner : ContainerRunner
{
    private const string DefaultCnxStringTemplate = "Server=localhost,##port##;User Id=sa;Password=localdevpassword#123;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;";

    public SqlServerContainerRunner()
        : base(@"run --rm -d -p :1433 -e ""SA_PASSWORD=localdevpassword#123"" -e ""ACCEPT_EULA=Y"" -e ""MSSQL_RPC_PORT=135"" mcr.microsoft.com/mssql/server:2022-latest")
    {
    }

    protected override Task OnStarted()
    {
        this.ConnectionString = DefaultCnxStringTemplate.Replace("##port##", this.Port.ToString());
        return Task.CompletedTask;
    }

    protected override async Task<bool> ReadinessProbe()
    {
        var connectionString = DefaultCnxStringTemplate.Replace("##port##", this.Port.ToString());
        using (var sqlConnection = new SqlConnection(connectionString))
        {
            try
            {
                await sqlConnection.OpenAsync();
                return true;
            }
            catch (SqlException)
            {
                return false;
            }
        }
    }

    public static string FormatCnxString(string port)
    {
        return DefaultCnxStringTemplate.Replace("##port##", port);
    }
}