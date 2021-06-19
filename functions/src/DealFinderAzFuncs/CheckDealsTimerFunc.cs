using System;
using System.Net.Http;
using System.Threading.Tasks;
using DF.Services.Html;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DealFinderAzFuncs
{
    public static class CheckDealsTimerFunc
    {
        [FunctionName("CheckDealsTimerFunc")]
        public async static Task Run([TimerTrigger("0 0 */3 * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var dealsFuncURI = config["DealFuncURI"];

            using(var httpclient = new HttpClient())
            {
                var res = await httpclient.GetAsync(dealsFuncURI);
                if (!res.IsSuccessStatusCode)
                {
                    log.LogError("Unable to process deals");
                }
            }
        }
    }
}
