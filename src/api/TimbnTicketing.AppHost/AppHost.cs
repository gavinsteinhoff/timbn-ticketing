using Projects;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var stripeSecretKey = builder.AddParameter("stripe-secret-key", secret: true);
var database = builder.AddConnectionString("Ticketing");

var api = builder
    .AddProject<TimbnTicketing_Api>("timbnticketing-api")
    .WithReference(database)
    .WithEnvironment("Stripe__SecretKey", stripeSecretKey);

builder
    .AddScalarApiReference(options =>
    {
        options.ForwardOriginalHostHeader();
    })
    .WithApiReference(api);

builder.Build().Run();
