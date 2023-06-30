using Azure.Storage.Blobs;
using DurableFunctionsDemo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        SetupBlobService(services);
    })
    .Build();

host.Run();

void SetupBlobService(IServiceCollection serviceCollection)
{
    var config = serviceCollection.BuildServiceProvider().GetService<IConfiguration>();
    var connectionString = config.GetValue<string>("AzureWebJobsStorage");
    var blobServiceClient = new BlobServiceClient(connectionString);
    serviceCollection.AddSingleton(blobServiceClient);

    serviceCollection.AddScoped<IBlobService, BlobService>();
}
