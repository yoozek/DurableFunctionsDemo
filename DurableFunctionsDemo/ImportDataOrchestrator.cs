using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsDemo
{
    public record ImportDataOrchestratorInput(DateOnly PriceListDate);
    public record ImportDataOrchestratorResult(List<ImportDataActivityResult> ImportResults);

    public class ImportDataOrchestrator
    {
        [Function(nameof(ImportDataOrchestrator))]
        public async Task<ImportDataOrchestratorResult> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var input = context.GetInput<ImportDataOrchestratorInput>() 
                        ?? throw new ArgumentNullException(nameof(ImportDataOrchestratorInput));

            var logger = context.CreateReplaySafeLogger(nameof(ImportDataOrchestrator));
            logger.LogInformation("Running orchestration for Importing Data");

            var importDataInput = new ImportDataActivityInput(input.PriceListDate);
            var importDataTasks = new List<Task<ImportDataActivityResult>>
            {
                CallImportDataActivity(context, importDataInput with { DelayInSeconds = 1}),
                CallImportDataActivity(context, importDataInput with { DelayInSeconds = 2}),
                CallImportDataActivity(context, importDataInput with { DelayInSeconds = 4}),
                CallImportDataActivity(context, importDataInput with { DelayInSeconds = 8})
            };

            var results = await Task.WhenAll(importDataTasks);

            return new ImportDataOrchestratorResult(results.ToList());
        }

        private static Task<ImportDataActivityResult> CallImportDataActivity(TaskOrchestrationContext context, ImportDataActivityInput importDataInput)
        {
            return context.CallActivityAsync<ImportDataActivityResult>(nameof(ImportDataActivity), importDataInput);
        }
    }

    public record ImportDataActivityInput(DateOnly PriceListDate, int DelayInSeconds = 1);

    public record ImportDataActivityResult(bool Success = true);

    public class ImportDataActivity
    {
        [Function(nameof(ImportDataActivity))]
        public static ImportDataActivityResult Run([ActivityTrigger] ImportDataActivityInput input, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(ImportDataActivityInput));

            // Import data time to complete simulation
            Thread.Sleep(input.DelayInSeconds * 1000);

            logger.LogInformation($"Data imported in {input.DelayInSeconds} seconds");

            return new ImportDataActivityResult();
        }
    }
}
