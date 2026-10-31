using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.Builder;
using ECommerce.Api.Endpoints;
using ECommerce.Api.ExceptionHandlers;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence.Seeding;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ECommerce API",
        Version = "v1",
        Description = "ECommerce API documentation"
    });
});

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd(
            "traceId",
            context.HttpContext.TraceIdentifier);
    });

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "ECommerce API v1");

    options.RoutePrefix = "swagger";
});

// if (app.Environment.IsDevelopment())
// {

await using var scope = app.Services.CreateAsyncScope();
var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

try
{
    await seeder.SeedAll();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database seeding failed. Make sure the database exists and is reachable.");
}
// }

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var versionedApi = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

versionedApi.MapAuthEndpoints();
versionedApi.MapProductEndpoints();
versionedApi.MapBrandEndpoints();
versionedApi.MapProductTypeEndpoints();
versionedApi.MapDeliveryMethodEndpoints();
versionedApi.MapOrderEndpoints();
versionedApi.MapBasketEndpoints();
versionedApi.MapUserAddressEndpoints();



app.Run();
