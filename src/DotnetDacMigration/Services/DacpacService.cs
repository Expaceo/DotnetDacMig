using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotnetDacMigration.Services
{
    internal interface IDacpacService
    {
        string GetSqlProjPath(string projectPath);
        Task<string?> BuildSqlProj(string projectPath);
        Task<string?> GenerateScript(string cnxString, string dbName, string dacpacPath);
        Task<IEnumerable<Operation>> GenerateReport(string cnxString, string dbName, string dacpacPath);
    }

    internal class DacpacService : IDacpacService
    {
        private readonly ILogger _logger;
        public DacpacService(ILogger logger)
        {
            _logger = logger;
        }
        public async Task<string?> BuildSqlProj(string projectPath)
        {
            var csprojPath = GetSqlProjPath(projectPath);
            var csproj = await File.ReadAllTextAsync(csprojPath);
            if (!csproj.Contains("<Content Remove=\"Migrations\\*.sql\" />"))
            {
                var itemGroupIndex = csproj.IndexOf("<ItemGroup>");
                csproj = csproj.Substring(0, itemGroupIndex + 11) + "\r\n<Content Remove=\"Migrations\\*.sql\" />" + csproj.Substring(itemGroupIndex + 11);
                await File.WriteAllTextAsync(csprojPath, csproj);
            }

            _logger.LogInformation("Building database project");
            var (stdout, stderr, code) = await ProcessLauncher.Run("dotnet", $"build {csprojPath} -c Release -o {projectPath}", projectPath);
            if (code != 0)
            {
                _logger.LogError("Failed building database project");
                _logger.LogError(stdout);
                _logger.LogError(stderr);
                return null;
            }
            
            _logger.LogDebug(stdout);
            var dacpac = Directory.EnumerateFiles(projectPath, "*.dacpac").FirstOrDefault();

            if (dacpac == null)
            {
                _logger.LogError("Unable to find dacpac file.");
                return null;
            }

            _logger.LogInformation("Successfully built database project");

            return dacpac;
        }

        public async Task<IEnumerable<Operation>> GenerateReport(string cnxString, string dbName, string dacpacPath)
        {
            _logger.LogInformation("Starting comparison");

            DacServices dacServices = new DacServices(cnxString);
            var result = dacServices.Script(DacPackage.Load(dacpacPath), dbName, new PublishOptions
            {
                GenerateDeploymentScript = false,
                GenerateDeploymentReport = true,
                DeployOptions = new DacDeployOptions
                {
                    DeployDatabaseInSingleUserMode = false,
                    GenerateSmartDefaults = false,
                    ScriptNewConstraintValidation = false,
                    VerifyDeployment = false,
                    BlockOnPossibleDataLoss = false,
                    DropConstraintsNotInSource = true,
                    DropDmlTriggersNotInSource = true,
                    DropIndexesNotInSource = true,
                    DropObjectsNotInSource = true,
                    IgnoreAnsiNulls = true,
                    IgnoreAuthorizer = true,
                    IgnoreFileAndLogFilePath = true,
                    IgnoreIndexOptions = true,
                    IgnoreWithNocheckOnForeignKeys = true,
                    IgnoreWithNocheckOnCheckConstraints = true,
                    IgnorePermissions = true,
                    IgnoreExtendedProperties = true,
                    IgnoreFullTextCatalogFilePath = true,
                    IgnoreSemicolonBetweenStatements = true,
                    IgnoreWhitespace = true,
                    CreateNewDatabase = false,
                }
            });

            _logger.LogInformation("Successfully generated comparison report");
            return ParseReport(result.DeploymentReport);
        }

        public async Task<string?> GenerateScript(string cnxString, string dbName, string dacpacPath)
        {
            _logger.LogInformation("Starting script generation");

            DacServices dacServices = new DacServices(cnxString);
            var result = dacServices.Script(DacPackage.Load(dacpacPath), dbName, new PublishOptions
            {
                GenerateDeploymentScript = true,
                GenerateDeploymentReport = true,
                DeployOptions = new DacDeployOptions
                {
                    DeployDatabaseInSingleUserMode = false,
                    GenerateSmartDefaults = false,
                    ScriptNewConstraintValidation = false,
                    VerifyDeployment = false,
                    BlockOnPossibleDataLoss = false,
                    DropConstraintsNotInSource = true,
                    DropDmlTriggersNotInSource = true,
                    DropIndexesNotInSource = true,
                    DropObjectsNotInSource = true,
                    IgnoreAnsiNulls = true,
                    IgnoreAuthorizer = true,
                    IgnoreFileAndLogFilePath = true,
                    IgnoreIndexOptions = true,
                    IgnoreWithNocheckOnForeignKeys = true,
                    IgnoreWithNocheckOnCheckConstraints = true,
                    IgnorePermissions = true,
                    IgnoreExtendedProperties = true,
                    IgnoreFullTextCatalogFilePath = true,
                    IgnoreSemicolonBetweenStatements = true,
                    IgnoreWhitespace = true,
                    CreateNewDatabase = false,
                }
            });

            var report = ParseReport(result.DeploymentReport);
            if (report.Count() == 0)
            {
                _logger.LogInformation("Script generation cancelled. No changes were detected.");
                return null;
            }

            _logger.LogInformation("Successfully generated script");
            return RemoveSqlPackagePrints(result.DatabaseScript);
        }

        public string GetSqlProjPath(string projectPath)
        {
            if (File.Exists(projectPath)) 
            {
                if (new FileInfo(projectPath).Extension == ".csproj")
                    return projectPath;
                else
                    throw new Exception($"{projectPath} is not a csproj file");
            }
            else if (Directory.Exists(projectPath))
            {
                var csprojPath = Directory.EnumerateFiles(projectPath, "*.csproj").FirstOrDefault();
                if (csprojPath == null)
                {
                    throw new Exception($"{projectPath} directory doesn't contain a csproj");
                }
                return csprojPath;
            }
            throw new Exception($"path {projectPath} doesn't exist");           
        }
    
    
        private IEnumerable<Operation> ParseReport(string report)
        {
            var doc = XDocument.Parse(report);
            var result = new List<Operation>();
            foreach (var rOp in doc.Descendants("{http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02}Operation"))
            {
                var items = new List<Item>();
                var name = rOp.Attribute("Name")?.Value ?? "";
                foreach (var rItem in rOp.Descendants("{http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02}Item"))
                {
                    var attr = rItem.Attribute("Value")?.Value;
                    if (attr != null && !attr.Contains(MigrationGenerationService.MigrationTableName))
                    {
                        items.Add(new Item(attr));
                    }
                }
                if (items.Count > 0)
                {
                    result.Add(new Operation(name, items));
                }
            }
            return result;
        }
        private string RemoveSqlPackagePrints(string script)
        {
            var regex = new Regex(@"PRINT N'.+';\s+GO");
            return regex.Replace(script, "");
        }
    }


    internal record Operation(string Name, IEnumerable<Item> Items);
    internal record Item(string Value);

}
