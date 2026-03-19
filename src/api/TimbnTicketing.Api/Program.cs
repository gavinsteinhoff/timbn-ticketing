using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TimbnTicketing.Api.Auth;
using TimbnTicketing.Api.Endpoints;
using TimbnTicketing.Api.Services;
using TimbnTicketing.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Ticketing")));

var firebaseProjectId = builder.Configuration["Auth:FirebaseProjectId"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<CurrentUserContext>();

builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<CurrentUserService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Firebase JWT token. Paste the token value only (no 'Bearer' prefix).",
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserResolverMiddleware>();
app.UseMiddleware<OrgResolutionMiddleware>();
app.UseMiddleware<MembershipResolutionMiddleware>();

// Auth (public — no JWT required)
app.MapGroup("/auth")
   .MapAuthEndpoints();

// Current user (requires auth)
app.MapGroup("/me")
   .MapCurrentUserEndpoints()
   .RequireAuthorization();

// Organizations (requires auth)
app.MapGroup("/orgs")
   .MapOrganizationEndpoints()
   .RequireAuthorization();

// Org-scoped resources (all require auth + membership)
app.MapGroup("/orgs/{orgSlug}/roles")
   .MapRoleEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/members")
   .MapMemberEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/metadata")
   .MapMetadataEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/venues")
   .MapVenueEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/events")
   .MapEventEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/ticket-types")
   .MapTicketTypeEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/events/{eventSlug}/tickets")
   .MapEventTicketEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/events/{eventSlug}/orders")
   .MapOrderEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/events/{eventSlug}/checkin")
   .MapCheckinEndpoints()
   .RequireAuthorization()
   .RequireMembership();

app.MapGroup("/orgs/{orgSlug}/discount-codes")
   .MapDiscountCodeEndpoints()
   .RequireAuthorization()
   .RequireMembership();

// Ticket claims (requires auth, not org-scoped)
app.MapGroup("/tickets/claim")
   .MapTicketClaimEndpoints()
   .RequireAuthorization();

// Ticket QR codes (requires auth)
app.MapGroup("/tickets")
   .MapTicketEndpoints()
   .RequireAuthorization();

app.Run();
