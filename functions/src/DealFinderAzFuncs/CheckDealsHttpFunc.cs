using DF.Services.Html;
using DF.Services.Models;
using DF.Services.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DealFinderAzFuncs
{
    public static class CheckDealsHttpFunc
    {
        [FunctionName("CheckDealsHttpFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function to get current deals executed.");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var keywords = config["Keywords"];
            var connStr = config["TableStorateConnectionString"];

            if (!Validate(keywords, connStr))
            {
                return new BadRequestObjectResult(new { status = "The Table Storage connection string (TableStorateConnectionString) or the search Kewords are missing in Application Settings." });
            }

            // Process the deals
            List<Deal> results = await ProcessDealsAsync(keywords, connStr);

            // If there are deals
            if (results.Count > 0)
            {
                // Send Email
                var emailURI = config["EmailServiceURI"];
                var emailsTo = config["EmailsTo"];
                List<Email> to = PrepareEmailAddresses(emailsTo);
                StringContent stringContent = PrepareContent(results, to);

                using (var client = new HttpClient())
                {
                    // Post to the email endpoint
                    var res = await client.PostAsync(emailURI, stringContent);
                    if (!res.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(new { status = $"Unable to email results{results.Count}." });
                    }
                }
            }

            if (results.Count > 0)
                return new OkObjectResult(new { status = $"Deals processed {results.Count}." });
            else
                return new OkObjectResult(new { status = "No deals processed." });
        }

        private static bool Validate(string keywords, string connStr)
        {
            return !(string.IsNullOrEmpty(keywords) || string.IsNullOrEmpty(connStr));
        }

        private static List<Email> PrepareEmailAddresses(string emailsTo)
        {
            var to = new List<Email>();
            ParseEmails(emailsTo, to);
            return to;
        }

        private static void ParseEmails(string emailsTo, List<Email> to)
        {
            foreach (var email in emailsTo.Split(","))
            {
                var nameAndEmail = email.Split("|");
                to.Add(new Email
                {
                    Name = nameAndEmail[0],
                    EmailAddress = nameAndEmail[1]
                });
            }
        }

        private static StringContent PrepareContent(List<Deal> results, List<Email> to)
        {
            var emailMessage = new EmailMessage
            {
                Subject = "New deal found",
                To = to.ToArray()
            };
            emailMessage.Body = BuildBody(results);
            var json = JsonConvert.SerializeObject(emailMessage);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return content;
        }

        private static async Task<List<Deal>> ProcessDealsAsync(string keywords, string connStr)
        {
            var stateService = await StateService.CreateAsync(connStr);
            var edealInfo = new ProcessEDealInfo();
            edealInfo.StateService = stateService;

            var techBargans = new ProcessTechBargains();
            techBargans.StateService = stateService;

            var dealNews = new ProcessTechBargains();
            dealNews.StateService = stateService;


            var taskEdealInfo = edealInfo.ProcessAsync(keywords);
            var taskTechBargains = techBargans.ProcessAsync(keywords);
            var taskDealNews = dealNews.ProcessAsync(keywords);

            // Execute tasks in parallel
            await Task.WhenAll(taskEdealInfo, taskTechBargains, taskDealNews);

            var results = taskEdealInfo.Result ?? new List<Deal>();
            results.AddRange(taskTechBargains.Result);
            results.AddRange(taskDealNews.Result);
            return results;
        }

        private static string BuildBody(List<Deal> results)
        {
            var sb = new StringBuilder();
            if (results.Count > 0)
            {
                sb.Append("<html><head><title></title>");
                sb.Append("<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.0.1/dist/css/bootstrap.min.css\" integrity=\"undefined\" crossorigin=\"anonymous\">");
                // sb.Append("<style>.btn { " +
                // "display: inline-block; " +
                // "font-weight: 400; " +
                // "line-height: 1.5; " +
                // "color:#212529; " +
                // "text-align: center; " +
                // "text-decoration: none; " +
                // "vertical-align: middle; " +
                // "cursor: pointer; " +
                // "-webkit-user-select: none; " +
                // "-moz-user-select: none; " +
                // "user-select: none; " +
                // "background-color: transparent; " +
                // "border: 1px solid transparent; " +
                // "padding: 0.375rem 0.75rem; " +
                // "font-size: 1rem; " +
                // "border-radius: 0.25rem; " +
                // "transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;}" +
                // ".btn-primary {" +
                // "color: #fff;" +
                // "background-color: #0d6efd;" +
                // "border-color: #0d6efd;}</style>");
                sb.Append("</head><body><div><table>");
                foreach (var deal in results)
                {
                    sb.Append("<tr><td align='center' style='padding:3px;'>");
                    
                    // TODO: Move to Eastearn time
                    sb.Append($"{DateTime.UtcNow}<br>");
                    sb.Append($"<a class=\"btn btn-primary\" href='{deal.Site}'>{deal.Domain}</a><br>");
                    if (!string.IsNullOrEmpty(deal.Vendor))
                        sb.Append($"{deal.Vendor}<br>");
                    sb.Append($"{deal.Description}<br>");
                    if (!string.IsNullOrEmpty(deal.Price))
                        sb.Append($"{deal.Price}<br>");

                    if (!string.IsNullOrEmpty(deal.Link))
                        //sb.Append($":{deal.Link}");
                        sb.Append($"<a class=\"btn btn-primary\" href=\"{deal.Link}\" target=\"_blank\">Get Deal</a><br>");

                    sb.Append("<hr></td></tr>");
                }
                sb.Append("</table><div></body></html>");
            }
            return sb.ToString();
        }
    }
}

