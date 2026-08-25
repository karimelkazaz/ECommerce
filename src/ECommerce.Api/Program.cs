using ECommerce.Api.Endpoints;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

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
}

app.UseHttpsRedirection();

app.MapProductEndpoints();
app.MapBrandEndpoints();
app.MapProductTypeEndpoints();

app.Run();
