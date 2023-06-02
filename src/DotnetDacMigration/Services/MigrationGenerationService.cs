using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetDacMigration.Services;

internal interface IMigrationGenerationService
{
    Task<bool> DeployMigrationsScripts(string cnxString, string dbName, string migrationScriptsPath, bool createDb = false);
    Task<string> CreateMigration(string migrationName, string migrationScriptsPath, string databaseScript);
    string GetInitMigrationScript(string dbName);
    Task<IEnumerable<string>> GetMigrationScriptStatements(string MigrationName, string migrationScriptsPath, string dbName);


}
class MigrationGenerationService : IMigrationGenerationService
{
    public const string MigrationTableName = "__DacpacMigrationsHistory";

    private readonly ILogger logger;
    public MigrationGenerationService(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task<bool> DeployMigrationsScripts(string cnxString, string dbName, string migrationScriptsPath, bool createDb = false)
    {
        using SqlConnection connection = new SqlConnection(cnxString);
        connection.InfoMessage += (sender, e) => logger.LogDebug(e.Message);
        await connection.OpenAsync();
        SqlCommand cmd;
        if (createDb)
        {
            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE [{dbName}]";
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException e)
            {
                logger.LogError("Error when creating the database : {message}", e.Message);
                return false;
            }
        }

        try
        {
            await connection.ChangeDatabaseAsync(dbName);

            var initScript = GetInitMigrationScript(dbName);
            cmd = connection.CreateCommand();
            cmd.CommandText = initScript;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException e)
        {
            logger.LogError("Error when creating migration history table : {message}", e.Message);
            return false;
        }

        foreach (var mig in Directory.EnumerateFiles(migrationScriptsPath, "*.sql"))
        {
            var migrationName = new FileInfo(mig).Name;
            migrationName = migrationName.Substring(0, migrationName.Length - 4);

            try
            {
                var statements = await GetMigrationScriptStatements(migrationName, migrationScriptsPath, dbName);
                foreach (var s in statements)
                {
                    cmd = connection.CreateCommand();
                    cmd.CommandText = s;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch(SqlException e)
            {
                logger.LogError("Error when applying migration {name} : {message}", migrationName, e.Message);
                return false;
            }
        }
        return true;
    }

    public async Task<string> CreateMigration(string migrationName, string migrationScriptsPath, string databaseScript)
    {
        string removeInstr = $@"DROP TABLE [dbo].[{MigrationTableName}];


GO";
        var fileName = $"{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}_{migrationName.Replace(" ", "-")}.sql";
        databaseScript = databaseScript.Substring(databaseScript.IndexOf(removeInstr) + removeInstr.Length);
        await File.WriteAllTextAsync(Path.Combine(migrationScriptsPath, fileName), databaseScript);
        return fileName;
    }

    public string GetInitMigrationScript(string dbName)
    {
        return $@"
IF OBJECT_ID(N'[{MigrationTableName}]') IS NULL
BEGIN
    CREATE TABLE [{MigrationTableName}] (
        [MigrationName] nvarchar(150) NOT NULL,
        [DeploymentDate] DATETIME NOT NULL DEFAULT(GETUTCDATE())
        CONSTRAINT [PK_{MigrationTableName}] PRIMARY KEY ([MigrationName])
    );
END;
";
    }

    public async Task<IEnumerable<string>> GetMigrationScriptStatements(string MigrationName, string migrationScriptsPath, string dbName)
    {
        var migrationScript = await File.ReadAllTextAsync(Path.Combine(migrationScriptsPath, MigrationName + ".sql"));
        var statements = migrationScript.Replace("$(DatabaseName)", dbName).Split("GO\r\n");
        var result = new List<string>();

        foreach (var s in statements.Where(s => !String.IsNullOrWhiteSpace(s)))
        {
            string statement = s.Trim();
            if (statement.Contains("CREATE ", StringComparison.InvariantCultureIgnoreCase) || statement.Contains("ALTER ", StringComparison.InvariantCultureIgnoreCase) || statement.Contains("DROP ", StringComparison.InvariantCultureIgnoreCase))
            {
                statement = $"EXEC ('{statement.Replace("'", "''")}')";
            }
            result.Add($@"
IF NOT EXISTS(SELECT * FROM [{MigrationTableName}] WHERE [MigrationName] = N'{MigrationName}')
BEGIN
    {statement}
END;
");
        }

        result.Add($@"
IF NOT EXISTS(SELECT * FROM [{MigrationTableName}] WHERE [MigrationName] = N'{MigrationName}')
BEGIN
    INSERT INTO [{MigrationTableName}] ([MigrationName])
    VALUES (N'{MigrationName}');
END;
");
        return result;
    }
}
