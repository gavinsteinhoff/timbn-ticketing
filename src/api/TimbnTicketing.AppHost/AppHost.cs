var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TimbnTicketing_Api>("timbnticketing-api");

builder.Build().Run();
