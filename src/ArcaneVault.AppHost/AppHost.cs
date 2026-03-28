
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.ArcaneVault_Web>("arcanevault-web");

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
    api.WaitFor(arcaneVaultDb);
}
else
{
    var azureSql = builder.AddAzureSqlServer("sql-server")
        .PublishAsConnectionString();

    api.WithReference(azureSql);
}

api.WithHttpHealthCheck("/alive")
   .WithHttpHealthCheck("/health");

builder.Build().Run();
