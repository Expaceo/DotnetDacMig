using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Options;

internal class DeployMigrationsOptions : OptionsBase
{
    public string? To { get; set; }
    public string DbName { get; set; }
    public string MigrationsPath { get; set; }
    public bool CreateDatabase { get; set; }

    public DeployMigrationsOptions(string? to, string dbName, string migrationsPath, bool createDatabase, string dbConnectionString) 
        : base(dbConnectionString) 
    { 
        To = to; 
        DbName = dbName;
        MigrationsPath = migrationsPath;
        CreateDatabase = createDatabase;
    }
}
