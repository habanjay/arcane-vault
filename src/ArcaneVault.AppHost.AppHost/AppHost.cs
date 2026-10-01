var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.ArcaneVault_AppHost_Server>("arcaneVault-server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("arcaneVault-client", "../ArcaneVault.Client")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
