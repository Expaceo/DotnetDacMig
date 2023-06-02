using DotnetDacMigration.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotnetDacMigration.Services;

interface ISqlServerService
{
    Task<string> GetSqlCnxString();
}
class SqlServerService : ISqlServerService
{
    public async Task<string> GetSqlCnxString()
    {
        var (stdout, stderr, code) = await ProcessLauncher.Run("docker", "ps --filter=\"ancestor=mcr.microsoft.com/mssql/server:2022-latest\" --format \"{{.Ports}}\"");
        if (code == 0)
        {
            var ports = stdout.Split("\n").FirstOrDefault() ?? "";
            Regex r = new Regex(@"[0-9.]+:(\d+)->1433/tcp");
            var m = r.Match(ports);
            if (m.Success)
            {
                return SqlServerContainerRunner.FormatCnxString(m.Groups[1].Value);
            }
        }
        var runner = new SqlServerContainerRunner();
        await runner.StartAsync();
        return runner.ConnectionString!;
    }
}
