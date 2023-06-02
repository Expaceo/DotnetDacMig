using DotnetDacMigration.Options;
using DotnetDacMigration.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Commands
{
    internal class CheckMigrationsCmdHandler : ICmdHandler<CheckMigrationsOptions>
    {
        private readonly ILogger logger;
        private readonly IDacpacService dacpacService;
        private readonly IMigrationGenerationService migrationService;
        private readonly ISqlServerService sqlServerService;
        public CheckMigrationsCmdHandler(ILogger logger, IDacpacService dacpacService, IMigrationGenerationService migrationService, ISqlServerService sqlServerService)
        {
            this.logger = logger;
            this.dacpacService = dacpacService;
            this.migrationService = migrationService;
            this.sqlServerService = sqlServerService;
        }

        public async Task<int> ExecuteAsync(CheckMigrationsOptions options)
        {
            var tmpDbName = "dacmig_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string? cnxString = null;
            string? dacpac = null;
            try
            {
                var migrationScriptsPath = Path.Combine(options.ProjectPath, "Migrations");
                cnxString = options.DbConnectionString ?? await sqlServerService.GetSqlCnxString();

                dacpac = await dacpacService.BuildSqlProj(options.ProjectPath);
                if (dacpac == null)
                {
                    return 1;
                }

                bool deployed = await migrationService.DeployMigrationsScripts(cnxString, tmpDbName, migrationScriptsPath, true);
                if (!deployed)
                {
                    return 1;
                }

                var report = await dacpacService.GenerateReport(cnxString, tmpDbName, dacpac);

                if (report.Count() == 0)
                {
                    logger.LogInformation("No changes detected");
                    return 0;
                }

                logger.LogWarning("Changes were detected.");
                foreach (var op in report)
                {
                    foreach (var it in op.Items)
                    {
                        logger.LogWarning("{operation} on {item}", op.Name, it.Value);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occured while checking migrations");
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

            return 1;
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
}
