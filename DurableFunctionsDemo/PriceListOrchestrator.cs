using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsDemo
{
    public record PriceListOrchestratorInput(DateOnly PriceListDate);
    public record PriceListOrchestratorResult(List<string> GeneratePriceListResults);
    public class PriceListOrchestrator
    {
        [Function($"{nameof(PriceListOrchestrator)}")]
        public async Task<PriceListOrchestratorResult> Run(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var input = context.GetInput<PriceListOrchestratorInput>() 
                        ?? throw new ArgumentNullException(nameof(PriceListOrchestratorInput));
            
            var importResult = await context.CallSubOrchestratorAsync<ImportDataOrchestratorResult>(
                nameof(ImportDataOrchestrator),
                  new ImportDataOrchestratorInput(input.PriceListDate));

            if (importResult.ImportResults.Any(x => !x.Success))
            {
                // TODO: Call SendEmailToAdminActivity - pass failed results to include in email message?
                throw new Exception("Some imports failed");
            }
            
            var generatePriceListResults = await GeneratePriceLists(context, input.PriceListDate);

            return new PriceListOrchestratorResult(generatePriceListResults.ToList());
        }

        private static Task<string[]> GeneratePriceLists(TaskOrchestrationContext context,
            DateOnly priceListDate)
        {
            
            var generatePriceListTasks =  Enumerable.Range(1, 10)
                .Select(x 
                    => context.CallActivityAsync<string>(nameof(GeneratePriceListActivity), 
                        new GeneratePriceListActivityInput(priceListDate))).ToList();

            return Task.WhenAll(generatePriceListTasks);
        }

        //TODO: Add OpenApi attributes
        [Function($"{nameof(PriceListOrchestrator)}{nameof(HttpStart)}")]
        public async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            //TODO: Get price list date from request

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(PriceListOrchestrator),
                new PriceListOrchestratorInput(new DateOnly(2023, 06, 23)));

            var logger = executionContext.GetLogger($"{nameof(PriceListOrchestrator)}{nameof(HttpStart)}");
            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }

    public record GeneratePriceListActivityInput(DateOnly PriceListDate);
    public class GeneratePriceListActivity
    {
        private readonly IBlobService _blobService;

        public GeneratePriceListActivity(IBlobService blobService)
        {
            _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        }

        [Function(nameof(GeneratePriceListActivity))]
        public async Task<string> Run([ActivityTrigger] GeneratePriceListActivityInput input, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(GeneratePriceListActivity));
            logger.LogInformation("Generating Price List for {PriceListGenerationDate:yyyy-MM-dd}.", input.PriceListDate);

            // Heavy computing simulation
            Thread.Sleep(Random.Shared.Next(1000, 2000)); 

            var priceListNumber = Random.Shared.Next();

            using var stream = new MemoryStream();
            await using var sw = new StreamWriter(stream);
            await sw.WriteLineAsync($"Price List number {priceListNumber}");
            await _blobService.UploadAsync(stream, "price-lists", $"{priceListNumber}.txt");

            logger.LogInformation("Price List {PriceListNumber} has been uploaded to BlobStorage.", priceListNumber);

            return $"Generated Price List #{priceListNumber}";
        }
    }
}
