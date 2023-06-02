using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Commands;

internal interface ICmdHandler<TOptions>
{
    Task<int> ExecuteAsync(TOptions options);
}
