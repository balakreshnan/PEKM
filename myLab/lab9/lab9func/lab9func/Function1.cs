using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace lab9func
{
    public static class Function1
    {

        private class WebApiRequest
        {
            public List<InputRecord> Values { get; set; }
        }

        private class InputRecord
        {
            public class InputRecordData
            {
                public string DateOfBirth { get; set; }
                public string Destination { get; set; }
                public string DepartureDate { get; set; }
                public string ReturnDate { get; set; }
            }

            public string RecordId { get; set; }
            public InputRecordData Data { get; set; }
        }
        private class OutputRecord
        {
            public class OutputRecordData
            {
                public string Scoredprobability { get; set; }
            }

            public class OutputRecordMessage
            {
                public string Message { get; set; }
            }

            public string RecordId { get; set; }
            public OutputRecordData Data { get; set; }
            public List<OutputRecordMessage> Errors { get; set; }
            public List<OutputRecordMessage> Warnings { get; set; }
        }

        private class WebApiResponse
        {
            public WebApiResponse()
            {
                this.values = new List<OutputRecord>();
            }

            public List<OutputRecord> values { get; set; }
        }
        private class MLApiResponse
        {
            public MLApiResponse()
            {
                this.Results = new Dictionary<string, List<Item>>();
            }
            public Dictionary<string, List<Item>> Results { get; set; }
        }

        public class Item
        {
            public Item()
            {
                this.items = new Dictionary<string, Dictionary<string, string>>();
            }
            public Dictionary<string, Dictionary<string, string>> items { get; set; }
        }

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Custom skill: C# HTTP trigger function processed a request.");

            // Read input, deserialize it and validate it.
            var data = GetStructuredInput(req.Body);
            if (data == null)
            {
                return new BadRequestObjectResult("The request schema does not match expected schema.");
            }

            // Calculate the response for each value.
            var response = new WebApiResponse();
            foreach (var record in data.Values)
            {
                if (record == null || record.RecordId == null) continue;

                OutputRecord responseRecord = new OutputRecord();
                responseRecord.RecordId = record.RecordId;

                try
                {
                    responseRecord.Data = InvokeRequestResponseService(record.Data).Result;
                }
                catch (Exception e)
                {
                    // Something bad happened, log the issue.
                    var error = new OutputRecord.OutputRecordMessage
                    {
                        Message = e.Message
                    };

                    responseRecord.Errors = new List<OutputRecord.OutputRecordMessage>
                    {
                        error
                    };
                }
                finally
                {
                    response.values.Add(responseRecord);
                }
            }

            return new OkObjectResult(response);
        }

        private static WebApiRequest GetStructuredInput(Stream requestBody)
        {
            string request = new StreamReader(requestBody).ReadToEnd();
            var data = JsonConvert.DeserializeObject<WebApiRequest>(request);
            return data;
        }

        private static async Task<OutputRecord.OutputRecordData> InvokeRequestResponseService(InputRecord.InputRecordData inputRecord)
        {
            var outputRecord = new OutputRecord.OutputRecordData();
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            using (var client = new HttpClient(handler))
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, List<Dictionary<string, string>>>() {
                        {
                            "data",
                            new List<Dictionary<string, string>>(){
                                new Dictionary<string, string>(){

                                    {
                                        "AgeOnDeparture", (DateTime.Parse(inputRecord.DepartureDate).Year -
                                                            DateTime.Parse(inputRecord.DateOfBirth).Year).ToString()

                                    },

                                    {
                                        "Destination", inputRecord.Destination
                                    },

                                    {
                                        "DepartureMonth", DateTime.Parse(inputRecord.DepartureDate).Month.ToString()
                                    },

                                    {
                                        "DurationOfStay_Days", DateTime.Parse(inputRecord.ReturnDate).
                                        Subtract(DateTime.Parse(inputRecord.DepartureDate)).TotalDays.ToString()
                                    },

                                    {
                                        "Claim", "1"
                                    },

                                }
                            }
                        },
                    }
                    //},
                    //GlobalParameters = new Dictionary<string, string>()
                    //{
                    //}
                };

                claims sendclaim = new claims();
                sendclaim.AgeOnDeparture = Math.Round(DateTime.Parse(inputRecord.DepartureDate).Subtract(DateTime.Parse(inputRecord.DateOfBirth)).TotalDays / 365, 0).ToString();
                sendclaim.DepartureMonth = DateTime.Parse(inputRecord.DepartureDate).Month.ToString();
                sendclaim.Destination = inputRecord.Destination;
                sendclaim.DurationOfStay_Days = DateTime.Parse(inputRecord.ReturnDate).Subtract(DateTime.Parse(inputRecord.DepartureDate)).TotalDays.ToString();
                string jsonstr = "{ \"data\" : [ ";
                jsonstr += JsonConvert.SerializeObject(sendclaim);
                jsonstr += "] }";

                //const string apiKey = Environment.GetEnvironmentVariable("APIKey"); // Replace this with the API key for the web service
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri("http://65.52.6.162:80/api/v1/service/lab9endpoint/score");
                // it will look something like new Uri("http://<IP>:80/api/v1/service/amlstudio-72b266000a424baca39bd8/score?api-version=2.0&format=swagger");
                // WARNING: The 'await' statement below can result in a deadlock
                // if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false)
                // so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)

                var requestString = JsonConvert.SerializeObject(scoreRequest);
                //var content = new StringContent(requestString, Encoding.UTF8, "application/json");
                var content = new StringContent(jsonstr, Encoding.UTF8, "application/json");
                

                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    //var responseObj = JsonConvert.DeserializeObject<MLApiResponse>(data);
                    //outputRecord.Scoredprobability = responseObj.Results["output1"][0].items["output1Item"]["Scored Probabilities"];
                    outputRecord.Scoredprobability = data.ToString();
                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));
                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());
                }

                return outputRecord;
            }
        }

    }

    public class claims
    {
        public string AgeOnDeparture { get; set; }
        public string DepartureMonth { get; set; }
        public string Destination { get; set; }
        public string DurationOfStay_Days { get; set; }
    }

    public class claimresult
    {
        public int[] result { get; set; }
    }

}
