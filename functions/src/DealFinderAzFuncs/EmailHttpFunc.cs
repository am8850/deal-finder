using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using DF.Services.Models;
using MimeKit;
using System.Linq;

namespace DealFinderAzFuncs
{
    public static class EmailHttpFunc
    {
        [FunctionName("EmailHttpFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var smtpHostName = config["SmtpHostName"];
            var smtpHostPort = config["SmptpHostPort"] ?? "587";
            var smtpUserEmailAddress = config["SmtpUserEmailAddress"];
            var smtpUserPassword = config["SmtpPassword"];
            var smtpUserName = config["FromName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var msg = JsonConvert.DeserializeObject<EmailMessage>(requestBody);

            try
            {
                var errorMessage = string.Empty;
                if (msg is null)
                    errorMessage += "bad request body\n";
                if (msg.To is null || msg.To.Count() == 0)
                    errorMessage += "Email To information is required";
                if (string.IsNullOrEmpty(msg.Subject))
                    errorMessage += "Email Subject is required";
                if (string.IsNullOrEmpty(msg.Body))
                    errorMessage += "Email Body is required";

                if (!string.IsNullOrEmpty(errorMessage))
                    throw new ApplicationException(errorMessage);

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(smtpUserName, smtpUserEmailAddress));

                var recipients = msg.To.ToList().Select(c => new MailboxAddress(c.Name, c.EmailAddress));
                message.To.AddRange(recipients);

                message.Subject = msg.Subject;
                message.Body = new TextPart("html") // or plain
                {
                    Text = msg.Body
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) =>
                    {
                        return true;
                    };
                    client.Connect(smtpHostName, int.Parse(smtpHostPort), false);

                    // Note: only needed if the SMTP server requires authentication
                    client.Authenticate(smtpUserEmailAddress, smtpUserPassword);

                    client.Send(message);

                    client.Disconnect(true);
                }
                return (ActionResult)new OkObjectResult(new { status = "Sent" });
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
