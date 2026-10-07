var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.ArcaneVault_Server>("arcaneVault-server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("https", url => url.Url = "/scalar")
    .WithUrlForEndpoint("http", url => url.Url = "/scalar");

var webfrontend = builder.AddViteApp("arcaneVault-client", "../ArcaneVault.Client")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
