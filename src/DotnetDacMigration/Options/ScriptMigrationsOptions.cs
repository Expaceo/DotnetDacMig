using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Options;

internal class ScriptMigrationsOptions
{
    public string? To { get; set; }
    public string DbName { get; set; }
    public string MigrationsPath { get; set; }
    public string? OutputFile { get; set; }
    public ScriptMigrationsOptions(string? to, string dbName, string migrationsPath, string outputFile) 
    { 
        To = to; 
        DbName = dbName;
        MigrationsPath = migrationsPath; 
        OutputFile = outputFile;
    }
}
