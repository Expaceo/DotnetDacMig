using DotnetDacMigration.Options;
using DotnetDacMigration.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotnetDacMigration.Commands;

internal class ScriptMigrationsCmdHandler : ICmdHandler<ScriptMigrationsOptions>
{
    private readonly IMigrationGenerationService migrationService;
    private readonly ILogger logger;

    public ScriptMigrationsCmdHandler(IMigrationGenerationService migrationService, ILogger logger)
    {
        this.migrationService = migrationService;
        this.logger = logger;
    }

    public async Task<int> ExecuteAsync(ScriptMigrationsOptions options)
    {
        var builder = new StringBuilder();

        var initScript = migrationService.GetInitMigrationScript(options.DbName);
        builder.Append(initScript);
        builder.AppendLine("GO");

        var migrations = Directory.EnumerateFiles(options.MigrationsPath, "*.sql")
            .Select(f => new FileInfo(f).Name)
            .Select(m => m.Substring(0, m.Length - 4))
            .ToList();

        if (options.To != null)
        {
            var toIndex = migrations.IndexOf(options.To);
            if (toIndex  != -1)
            {
                migrations = migrations.Take(toIndex + 1).ToList();
            }
        }

        foreach (var mig in migrations)
        {
            var statements = await migrationService.GetMigrationScriptStatements(mig, options.MigrationsPath, options.DbName);
            foreach (var s in statements)
            {
                builder.Append(s);
                builder.AppendLine("GO");
            }
        }

        var fileName = options.OutputFile ?? "./migrations.sql";
        await File.WriteAllTextAsync(fileName, builder.ToString());
        logger.LogInformation("Successfully generated deployment script to {fileName}", fileName);

        return 0;
    }
}
