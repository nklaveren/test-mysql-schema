using Aspire.Hosting;
using Aspire.Hosting.MySql;

var builder = DistributedApplication.CreateBuilder(args);

var mysqlServer = builder.AddMySql("mysql");
var schemaTestDatabase = mysqlServer.AddDatabase("schematest");

builder.AddProject<Projects.SchemaTest_Api>("schematest-api")
    .WithReference(schemaTestDatabase)
    .WithExternalHttpEndpoints();

builder.Build().Run();

