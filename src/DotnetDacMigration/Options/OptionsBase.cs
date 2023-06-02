using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Options;

internal abstract class OptionsBase
{
    public string? DbConnectionString { get; set; }

    public OptionsBase(string? dbConnectionString)
    {
        this.DbConnectionString = dbConnectionString;
    }
}
