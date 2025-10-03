using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SchemaTest.Api.Data;

public sealed class DatabaseInitializer(IServiceScopeFactory scopeFactory, ILogger<DatabaseInitializer> logger) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<DatabaseInitializer> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SchemaTestDbContext>();

        _logger.LogInformation("Ensuring SchemaTest database is created and seeded...");

        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (await context.Customers.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already contains data, skipping seed.");
            return;
        }

        var sampleCustomers = new[]
        {
            new Models.Customer("Ada Lovelace", "ada@example.com"),
            new Models.Customer("Grace Hopper", "grace@example.com")
        };

        context.Customers.AddRange(sampleCustomers);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} sample customers.", sampleCustomers.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
