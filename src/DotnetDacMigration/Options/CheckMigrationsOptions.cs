using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Options;

internal class CheckMigrationsOptions : OptionsBase
{
    public string ProjectPath { get; set; }

    public CheckMigrationsOptions(string projectPath, string? dbConnectionString) : base(dbConnectionString)
    {
        this.ProjectPath = projectPath;
    }
}
