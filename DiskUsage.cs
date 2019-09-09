using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using Microsoft.IdentityModel.Clients.ActiveDirectory;


namespace Honeywell.Function
{
    public static class DiskUsage
    {
        [FunctionName("DiskUsage")]
        // public static async Task<IActionResult> Run(
        //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        //     ILogger log)
         public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            
            log.LogInformation("C# HTTP trigger function processed a request.");

            double currentusage = 0;

            double diskLimit = double.Parse(Environment.GetEnvironmentVariable("disklimit"));

           

             string tenantId = "";   
            string clientId = "";
         string clientSecret = "";
            string subscription = "";
             string resourcegroup = "";
            string webfarm = "";
               string apiversion = "2018-02-01";

            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientSecret);
            var result = authenticationContext.AcquireTokenAsync(resource: "https://management.azure.com/", clientCredential: credential).Result;
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }
            string token = result.AccessToken;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(string.Format("https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverfarms/{2}/usages?api-version={3}", subscription, resourcegroup, webfarm, apiversion));
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer " + token;
            request.ContentType = "application/json";

            //Get the response
            var httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                string jsonResponse = streamReader.ReadToEnd();
                dynamic ob = JsonConvert.DeserializeObject(jsonResponse);
                dynamic re = ob.value.Children();

                foreach (var item in re)
                {
                    if (item.name.value == "FileSystemStorage")
                    {
                        currentusage = (double)item.currentValue / 1024 / 1024 / 1024;

                    }
                }
            }
         if(currentusage > diskLimit)
            log.LogInformation("Size of the disk is {itemKey} GB", currentusage);
          //  return  (ActionResult)new OkObjectResult($"Disk Size (in GB), {currentusage}");
                
        }
    }
}
