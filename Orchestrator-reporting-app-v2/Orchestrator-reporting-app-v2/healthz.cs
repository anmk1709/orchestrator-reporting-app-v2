using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;

namespace Orchestrator_reporting_app_v2
{
    public static class healthz
    {
        [FunctionName("healthz")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string env = req.Query["env"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            env = env ?? data?.env;

            if (String.IsNullOrEmpty(env))
            {
                env = "prod";
            }

            var appsToCheck = new List<string>();

            appsToCheck.Add(string.Format("https://hr-webforms-{0}.azure.dsb.dk/healthz",env));
            appsToCheck.Add(string.Format("https://robotics-developer-api-{0}.azure.dsb.dk/healthz", env));
            appsToCheck.Add(string.Format("https://transaction-items-{0}.azure.dsb.dk/api/healthz", env));

            string responseMessage = "";
            appsToCheck.ForEach(app => responseMessage = responseMessage + healthCheck(app) + "\r\n"); 

            //StringBuilder reponse = appsToCheck.Aggregate(new StringBuilder(), (a, b) => a.AppendLine(b));

            //string responseMessage = string.Format("test");

            return new OkObjectResult(responseMessage);
        }

        public static string healthCheck(string app)
        {
            HttpWebRequest healthzCheck = (HttpWebRequest)WebRequest.Create(app);
            healthzCheck.Timeout = 15000;
            healthzCheck.Method = "Head";

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)healthzCheck.GetResponse())
                {
                    string returnString = new OkObjectResult(response.StatusCode).ToString();
                    return string.Format("{0} was available, returned {1}",app, returnString); 
                }
            }
            catch(Exception ex)
            {
                return string.Format("{0} did not respond, returned the following error", app,ex.ToString());
            };
        }
    }
}
