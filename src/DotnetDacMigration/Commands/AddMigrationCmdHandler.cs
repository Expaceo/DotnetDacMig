using DotnetDacMigration.Options;
using DotnetDacMigration.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Commands;

class AddMigrationCmdHandler : ICmdHandler<AddMigrationOptions>
{
    private readonly ILogger logger;
    private readonly IDacpacService dacpacService;
    private readonly IMigrationGenerationService migrationService;
    private readonly ISqlServerService sqlServerService;
    public AddMigrationCmdHandler(ILogger logger, IDacpacService dacpacService, IMigrationGenerationService migrationService, ISqlServerService sqlServerService)
    {
        this.logger = logger;
        this.dacpacService = dacpacService;
        this.migrationService = migrationService;
        this.sqlServerService = sqlServerService;
    }

    public async Task<int> ExecuteAsync(AddMigrationOptions options)
    {
        string? cnxString = null;
        string? dacpac = null;
        var tmpDbName = "dacmig_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        try
        {
            var migrationScriptsPath = Path.Combine(options.ProjectPath, "Migrations");
            if (!Directory.Exists(migrationScriptsPath))
            {
                Directory.CreateDirectory(migrationScriptsPath);
            }

            cnxString = options.DbConnectionString ?? await sqlServerService.GetSqlCnxString();

            dacpac = await dacpacService.BuildSqlProj(options.ProjectPath);

            if (dacpac == null)
            {
                return 1;
            }

            var deployed = await migrationService.DeployMigrationsScripts(cnxString, tmpDbName, migrationScriptsPath, true);
            if (!deployed)
            {
                return 1;
            }

            string? dbScript = await dacpacService.GenerateScript(cnxString, tmpDbName, dacpac);
            if (dbScript == null)
            {
                return 0;
            }

            var migrationFile = await migrationService.CreateMigration(options.MigrationName, migrationScriptsPath, dbScript);
            logger.LogInformation("Successfully added migration {migraitonName}", migrationFile);

            return 0;

        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occured while adding migration");
            return 1;
        }
        finally
        {
            if (dacpac != null)
            {
                File.Delete(dacpac);
            }

            if (cnxString != null)
            {
                await CleanUp(cnxString, tmpDbName);
            }
        }
    }

    private async Task CleanUp(string cnxString, string tmpDbName)
    {

        using SqlConnection connection = new SqlConnection(cnxString);
        await connection.OpenAsync();
        await connection.ChangeDatabaseAsync("master");

        var cmd = connection.CreateCommand();
        cmd.CommandText = $"DROP DATABASE [{tmpDbName}]";
        await cmd.ExecuteNonQueryAsync();
    }
}
