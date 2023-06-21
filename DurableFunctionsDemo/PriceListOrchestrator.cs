using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsDemo
{
    public class PriceListOrchestrator
    {
        [Function($"{nameof(PriceListOrchestrator)}")]
        public async Task<List<string>> Run(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var importDataResult = await ImportData(context);

            return await GeneratePriceLists(context);
        }



        private async Task<List<string>> GeneratePriceLists(TaskOrchestrationContext context)
        {
            var generatePriceListInput = new GeneratePriceListActivityInput(new DateOnly(2022, 1, 1));
            var generatePriceListResults = new List<string>
            {
                await context.CallActivityAsync<string>(nameof(GeneratePriceListActivity), generatePriceListInput),
                await context.CallActivityAsync<string>(nameof(GeneratePriceListActivity), generatePriceListInput),
                await context.CallActivityAsync<string>(nameof(GeneratePriceListActivity), generatePriceListInput)
            };

            return generatePriceListResults;
        }

        [Function($"{nameof(PriceListOrchestrator)}{nameof(HttpStart)}")]
        public async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(PriceListOrchestrator));

            var logger = executionContext.GetLogger("Function1_HttpStart");
            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }

    public record GeneratePriceListActivityInput(DateOnly PriceListDate);
    public class GeneratePriceListActivity
    {
        [Function(nameof(GeneratePriceListActivity))]
        public static string Run([ActivityTrigger] GeneratePriceListActivityInput input, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(GeneratePriceListActivity));
            logger.LogInformation("Generating Price List for {PriceListGenerationDate:yyyy-MM-dd}.", input.PriceListDate);
            var priceListNumber = new Random().Next().ToString();

            return $"PriceList{priceListNumber} generated";
        }
    }
}
