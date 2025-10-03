using Aspire.Hosting;
using Aspire.Hosting.MySql;

var builder = DistributedApplication.CreateBuilder(args);

var mysqlServer = builder.AddMySql("mysql");
var schemaTestDatabase = mysqlServer.AddDatabase("schemadb");

builder.AddProject<Projects.SchemaTest_Api>("schematest-api")
    .WithReference(schemaTestDatabase);


// add mysql sgdb
schemaTestDatabase.OnConnectionStringAvailable((connectionString, _, getConnectionString) =>
{
    Console.WriteLine("Connection String: ");
    Console.WriteLine(connectionString);
    return Task.CompletedTask;
});

builder.Build().Run();