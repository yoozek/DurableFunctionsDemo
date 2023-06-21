using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsDemo
{
    public record ImportDataOrchestratorInput(DateOnly PriceListDate);
    public static class ImportDataOrchestrator
    {
        [Function(nameof(ImportDataOrchestrator))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger(nameof(ImportDataOrchestrator));
           
            var importDataInput = new ImportDataActivityInput(new DateOnly(2022, 1, 1), 10);
            var generatePriceListResults = new List<Task<ImportDataActivityResult>>
            {
                context.CallActivityAsync<ImportDataActivityResult>(nameof(ImportDataActivity), importDataInput),
                context.CallActivityAsync<ImportDataActivityResult>(nameof(ImportDataActivity), importDataInput),
                context.CallActivityAsync<ImportDataActivityResult>(nameof(ImportDataActivity), importDataInput)
            };

            await Task.WhenAll(generatePriceListResults)

            return generatePriceListResults;
        }

    }

    public record ImportDataActivityInput(DateOnly PriceListDate, int DelayInSeconds);
    public record ImportDataActivityResult(DateOnly PriceListDate);

    public class ImportDataActivity
    {
        [Function(nameof(ImportDataActivity))]
        public static string Run([ActivityTrigger] ImportDataActivityInput input, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(ImportDataActivityInput));
            logger.LogInformation("Generating Price List for {PriceListGenerationDate:yyyy-MM-dd}.", input.PriceListDate);
            var priceListNumber = new Random().Next().ToString();

            return $"PriceList{priceListNumber} generated";
        }
    }
}
