# Dotnet Dacpac Migrations

[![Version](https://img.shields.io/nuget/vpre/DotnetDacMigration.svg)](https://www.nuget.org/packages/DotnetDacMigration)
[![Downloads](https://img.shields.io/nuget/dt/DotnetDacMigration.svg)](https://www.nuget.org/packages/DotnetDacMigration)

DacMig is a dotnet tool used to create migration scripts for Sql projects.
This tool is only compatible with SQL projects that uses the [.NET SDK](https://www.nuget.org/packages/Microsoft.Build.Sql) and it requires .NET 6 runtime.
## Installation

### Install globally 
```
dotnet tool install --global DotnetDacMigration 
```

### Install locally

Add the dotnet tool manifest to your project :
```
dotnet new tool-manifest
```

Install the dacmig tool to your project
```
dotnet tool install DotnetDacMigration 
```

## Usage

The add and check commands require an instance of SQL Server on which to create a database and use it to compare the migrations to the project database. By default the tool will use docker to mount a container of SQL Server and use it for this purpose. But it can use another SQL instance provider by the `--connection-string` parameter. 

### add
Adds a new migration to the SQL project. If there are no changes to the database project since the last migration, no migration is created.
```
dotnet dacmig add "my migration" --project-path "./Path/To/the/sql/project"
```

### check
Checks whether the sql project is in sync with the migrations.
```
dotnet dacmig check --project-path "./Path/To/the/sql/project"
```

Returns an exit code of 1 if an error occured while applying the migration scripts or if changes are detected between the database project and the migrations.
This command can be used in a CI environnement to validate the migration scripts.

### deploy
Apply the migrations to a target database
```
dotnet dacmig deploy --db-name "myDb" --migrations-path "./Path/To/the/migrations/folder" --target-connection-string "Server=127.0.0.1;User ID=sa;Password=.."
```

### script
Generates an idempotent SQL script containing all migrations
```
dotnet dacmig script --db-name "myDb" --migrations-path "./Path/To/the/migrations/folder" --output "./migrations.sql"
```