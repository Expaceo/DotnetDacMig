using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Options;

internal class AddMigrationOptions : OptionsBase
{
    public string MigrationName { get; set; }
    public string ProjectPath { get; set; }

    public AddMigrationOptions(string name, string projectPath, string? dbConnectionString) : base(dbConnectionString)
    {
        this.MigrationName = name;
        this.ProjectPath = projectPath;
    }
}
