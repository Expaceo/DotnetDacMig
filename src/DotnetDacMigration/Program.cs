using DotnetDacMigration.Commands;
using DotnetDacMigration.Options;
using DotnetDacMigration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;

namespace DotnetDacMigration;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var root = new RootCommand(@"$dotnet dacpac migrations");
        root.Name = "dacmig";

        var addMigCmd = new Command("add", "Add a new migration");
        addMigCmd.AddArgument(new Argument<string>("name", "name of the migration"));
        addMigCmd.AddOption(new Option<string>(new string[] { "-p", "--project-path" }, () => ".", "path to the database project") { IsRequired = false });
        addMigCmd.AddOption(new Option<string?>(new string[] { "-c", "--connection-string" }, "connection string to a database used internally.") { IsRequired = false });

        addMigCmd.Handler = CommandHandler.Create<AddMigrationOptions, IHost>(Run);
        root.Add(addMigCmd);

        var checkMigCmd = new Command("check", "Checks if the project and migrations are in sync.");
        checkMigCmd.AddOption(new Option<string>(new string[] { "-p", "--project-path" }, () => ".", "path to the database project") { IsRequired = false });
        checkMigCmd.AddOption(new Option<string?>(new string[] { "-c", "--connection-string" }, "connection string to a database used internally.") { IsRequired = false });

        checkMigCmd.Handler = CommandHandler.Create<CheckMigrationsOptions, IHost>(Run);
        root.Add(checkMigCmd);

        var deployMigCmd = new Command("deploy", "Deploy migrations to a database");
        deployMigCmd.AddArgument(new Argument<string?>("to", "The target migration. Defaults to the last migration.") { Arity = ArgumentArity.ZeroOrOne });
        deployMigCmd.AddOption(new Option<string>(new string[] { "-d", "--db-name" }, "Name of the target database") { IsRequired = true });
        deployMigCmd.AddOption(new Option<string>(new string[] { "-m", "--migrations-path" }, () => "./Migrations", "path to migration scripts folder") { IsRequired = false });
        deployMigCmd.AddOption(new Option<string?>(new string[] { "-t", "--target-connection-string" }, "connection string to the target database") { IsRequired = true });
        deployMigCmd.AddOption(new Option<bool>(new string[] { "-c", "--create-database" }, () => false, "Create the database.") { IsRequired = false });

        deployMigCmd.Handler = CommandHandler.Create<DeployMigrationsOptions, IHost>(Run);
        root.Add(deployMigCmd);

        var scriptMigCmd = new Command("script", "Generate the deployment script");
        scriptMigCmd.AddArgument(new Argument<string?>("to", "The target migration. Defaults to the last migration.") { Arity = ArgumentArity.ZeroOrOne });
        scriptMigCmd.AddOption(new Option<string>(new string[] { "-d", "--db-name" }, "Name of the target database") { IsRequired = true });
        scriptMigCmd.AddOption(new Option<string>(new string[] { "-m", "--migrations-path" }, () => "./Migrations", "path to migration scripts folder") { IsRequired = false });
        scriptMigCmd.AddOption(new Option<string>(new string[] { "-o", "--output" }, () => "migration.sql", "path to output sql file") { IsRequired = false });

        scriptMigCmd.Handler = CommandHandler.Create<ScriptMigrationsOptions, IHost>(Run);
        root.Add(scriptMigCmd);

        var builder = new CommandLineBuilder(root);
        return await builder.UseHost(
            _ => Host.CreateDefaultBuilder()
            .ConfigureLogging(b => 
            {
                b.AddFilter("Microsoft.Hosting", b => false);
                b.AddConsole(options => options.FormatterName = "myConsoleFormatter")
                    .AddConsoleFormatter<MyConsoleFormatter, SimpleConsoleFormatterOptions>();
            }),
                host =>
                {
                    host.ConfigureServices(services =>
                    {
                        services.AddSingleton<IDacpacService, DacpacService>();
                        services.AddSingleton<IMigrationGenerationService, MigrationGenerationService>();
                        services.AddSingleton<ISqlServerService, SqlServerService>();
                        services.AddSingleton<ICmdHandler<AddMigrationOptions>, AddMigrationCmdHandler>();
                        services.AddSingleton<ICmdHandler<CheckMigrationsOptions>, CheckMigrationsCmdHandler>();
                        services.AddSingleton<ICmdHandler<DeployMigrationsOptions>, DeployMigrationsCmdHandler>();
                        services.AddSingleton<ICmdHandler<ScriptMigrationsOptions>, ScriptMigrationsCmdHandler>();
                        services.AddSingleton<ILogger>(serv =>
                        {
                            var fact = serv.GetRequiredService<ILoggerFactory>();
                            return fact.CreateLogger("");
                        });
                    });
                })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
    }


    static async Task<int> Run<TOptions>(TOptions options, IHost host)
    {
        var serviceProvider = host.Services;
        var cmdHandler = serviceProvider.GetRequiredService<ICmdHandler<TOptions>>();
        return await cmdHandler.ExecuteAsync(options);
    }
}