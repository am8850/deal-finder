using System;
using System.Threading.Tasks;
using DF.Services.State;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DealFinderAzFuncs
{
    public static class CleanupTimerFunc
    {
        [FunctionName("CleanupTimerFunc")]
        public async static Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            // Delete old data
            log.LogInformation($"C# Timer trigger function to cleanup table executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            try
            {
                var connStr = config["TableStorateConnectionString"];
                var stateService = await StateService.CreateAsync(connStr);
                var count = await stateService.CleanupAsync();
                if (count>0)
                {
                    log.LogError($"Records deleted: {count}");
                }
            }
            catch (Exception e)
            {
                log.LogError($"Error processing cleanup: {e.Message}");
            }
        }
    }
}
