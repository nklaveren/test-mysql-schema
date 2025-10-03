using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using SchemaTest.Api.Data;
using SchemaTest.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SchemaTestDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("schemadb")
        ?? throw new InvalidOperationException("A connection string named 'schematest' was not provided by Aspire.");

    var serverVersion = MySqlServerVersion.LatestSupportedServerVersion;

    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure();
        mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, entity) => $"{schema}_{entity}");
    });
});

builder.Services.AddHostedService<DatabaseInitializer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SchemaTest v1");
        options.DocumentTitle = "SchemaTest API";
    });

    app.MapOpenApi();
}

app.MapGet("/customers", async (SchemaTestDbContext context, CancellationToken cancellationToken) =>
        await context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Id)
            .ToListAsync(cancellationToken))
    .WithName("GetCustomers")
    .WithSummary("Lists the customers persisted in MySQL.");

app.MapPost("/customers", async (SchemaTestDbContext context, Customer customer, CancellationToken cancellationToken) =>
    {
        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);
        return Results.Created($"/customers/{customer.Id}", customer);
    })
    .WithName("CreateCustomer")
    .WithSummary("Creates a new customer and stores it in MySQL.")
    .WithOpenApi();

app.MapGet("/db-check", async (SchemaTestDbContext context, CancellationToken cancellationToken) =>
    {
        var canConnect = await context.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "Up" })
            : Results.Problem("Unable to connect to the MySQL container provisioned by Aspire.");
    })
    .WithName("DatabaseCheck")
    .WithSummary("Verifies the application can connect to MySQL.")
    .WithOpenApi();

app.MapDefaultEndpoints();

app.Run();
