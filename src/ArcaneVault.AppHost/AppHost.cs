
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

const string dbName = "arcanevaultdb";

if (builder.Environment.IsDevelopment())
{
    var sql = builder.AddSqlServer("sql-server")
        .WithHostPort(55251)
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent);

    var initScriptPath = Path.Join(Path.GetDirectoryName(typeof(Program).Assembly.Location), "init.sql");
    var arcaneVaultDb = sql.AddDatabase(dbName)
        .WithCreationScript(File.ReadAllText(initScriptPath));
    
    builder.AddProject<Projects.ArcaneVault_Web>("arcanevault-web")
        .WithReference(arcaneVaultDb)
        .WaitFor(arcaneVaultDb)
        .WithHttpHealthCheck("/alive")
        .WithHttpHealthCheck("/health");
}

builder.Build().Run();
