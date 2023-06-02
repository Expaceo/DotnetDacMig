using DotnetDacMigration.Options;
using DotnetDacMigration.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Commands;

internal class DeployMigrationsCmdHandler : ICmdHandler<DeployMigrationsOptions>
{
    private readonly IMigrationGenerationService migrationService;

    public DeployMigrationsCmdHandler(IMigrationGenerationService migrationService)
    {
        this.migrationService = migrationService;
    }

    public async Task<int> ExecuteAsync(DeployMigrationsOptions options)
    {
        await this.migrationService.DeployMigrationsScripts(options.DbConnectionString, options.DbName, options.MigrationsPath, options.CreateDatabase);
        return 0;
    }
}
