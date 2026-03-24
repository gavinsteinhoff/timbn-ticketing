using Projects;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<TimbnTicketing_Api>("timbnticketing-api");

builder
    .AddScalarApiReference(options =>
    {
        options.ForwardOriginalHostHeader();
    })
    .WithApiReference(api);

builder.Build().Run();
