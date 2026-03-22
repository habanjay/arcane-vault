var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ArcaneVault_Web>("arcanevault-web");

builder.Build().Run();
