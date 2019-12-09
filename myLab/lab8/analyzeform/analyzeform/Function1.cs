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
using System.Net.Http;
using System.Net;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace analyzeform
{
    public static class Function1
    {
        //private static readonly string formsRecognizerApiEndpoint =
            //"https://westus2.api.cognitive.microsoft.com/formrecognizer/v1.0-preview/custom";
        private static readonly string formsRecognizerApiEndpoint =
            "https://bbopenhackform.cognitiveservices.azure.com/";
        private static readonly string formsRecognizerApiKeySetting = "830151aca823469bb23f3c7737f0dd3e";
        private static readonly string modelIdSetting = "74361cbf-5417-497d-a91b-8950894acf3c";

        #region Class used to deserialize the request
        private class InputRecord
        {
            public class InputRecordData
            {
                public string formUrl;
                public string formSasToken;
            }

            public string RecordId { get; set; }
            public InputRecordData Data { get; set; }
        }

        private class WebApiRequest
        {
            public List<InputRecord> Values { get; set; }
        }
        #endregion

        #region Classes used to serialize the response
        private class OutputRecord
        {
            public class OutputRecordData
            {
                public string Name { get; set; }
                public string DestinationCity { get; set; }

                public string DepartureDate { get; set; }

                public string ReturnDate { get; set; }

                public string DateofBirth { get; set; }
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
        #endregion

        #region Classes used to interact with the Forms Recognizer Analyze API
        private class FormsRecognizerResponse
        {
            public string Status;
            public Page[] pages { get; set; }
        }

        private class Page
        {
            public int Number { get; set; }
            public int Height { get; set; }
            public int Width { get; set; }
            public int ClusterId { get; set; }

            public KeyValuePair[] KeyValuePairs { get; set; }

            public class KeyValuePair
            {
                public BoundedElement[] Key { get; set; }
                public BoundedElement[] Value { get; set; }
            }

            public class BoundedElement
            {
                public string Text { get; set; }
                public double[] BoundingBox { get; set; }

                public double confidence { get; set; }
            }
        }
        #endregion

        //Address:
        //City:
        //Country:
        //Date of Birth:
        //Date:
        //Departure Date:
        //Destination City:
        //Destination Country:
        //Humongous Insurance Corp
        //Name:
        //Personal Details
        //Postal Code:
        //Return Date:
        //Signature:
        //Travel Insurance Application Form for Margie's Travel Customers
        //Trip Details

        // Modify this list of fields to extract according to your requirements:
        private static readonly Dictionary<string, string> fieldMappings = new Dictionary<string, string> {
            { "Address:", "address" },
            { "City:", "recipient" },
            { "Country:", "recipient" },
            { "Date of Birth:", "recipient" },
            { "Date:", "recipient" },
            { "Departure Date:", "recipient" },
            { "Destination City:", "recipient" },
            { "Destination Country:", "recipient" },
            { "Name:", "recipient" },
            { "Postal Code:", "recipient" },
            { "Return Date:", "recipient" },
            { "Signature:", "recipient" }
        };
        // Modify this content type if you need to handle file types other than PDF
        private static readonly string contentType = "application/pdf";

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext executionContext)
        {
            log.LogInformation("Analyze Form Custom Skill: C# HTTP trigger function processed a request.");

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
                    responseRecord.Data = AnalyzeForm(record.Data).Result;
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

        async static Task<OutputRecord.OutputRecordData> AnalyzeForm(InputRecord.InputRecordData inputRecord)
        {
            string base_url = formsRecognizerApiEndpoint + @"/formrecognizer/v1.0-preview/custom";
            //string base_url = formsRecognizerApiEndpoint;
            //string fileUrl = DecodeBase64String(inputRecord.formUrl);
            string fileUrl = inputRecord.formUrl;
            string sasToken = inputRecord.formSasToken;

            //string fileUrl = "https://bbopenhackstore.blob.core.windows.net/insuranceformsall/Insurance Form 01.pdf";
            //string sasToken = "?sv=2019-02-02&ss=bfqt&srt=sco&sp=rwdlacup&se=2020-01-01T02:00:23Z&st=2019-12-08T18:00:23Z&spr=https&sig=CKDwmtl57dEoMqjJLGcRRTGREm7mwchpoHckcdnXrMM%3D";

            string completeurl = "https://bbopenhackstore.blob.core.windows.net/insuranceformsall/Insurance Form 01.pdf?sv=2019-02-02&ss=bfqt&srt=sco&sp=rwdlacup&se=2019-12-28T02:48:10Z&st=2019-12-06T18:48:10Z&spr=https&si";

            var outputRecord = new OutputRecord.OutputRecordData();
            byte[] bytes = null;

            if(fileUrl.Contains(""))
            {
                using (WebClient client = new WebClient())
                {
                    // Read the form to analyze
                    bytes = client.DownloadData(fileUrl + sasToken);
                }

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage())
                {
                    var url = base_url + "/models/" + modelIdSetting + "/analyze";

                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(url);
                    request.Content = new ByteArrayContent(bytes);
                    //request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    request.Headers.Add("Ocp-Apim-Subscription-Key", formsRecognizerApiKeySetting);

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic data = JsonConvert.DeserializeObject(responseBody);

                        var result = JsonConvert.DeserializeObject<FormsRecognizerResponse>(responseBody);

                        var name = GetField(result, "Name", 0);
                        var city = GetField(result, "Destination City", 0);
                        var depDate = GetField(result, "Departure Date", 0);

                        //issue where the field extracted by the recognizer is missing 'Re'
                        var returnDate = GetField(result, "Return Date", 0);
                        if (returnDate == null)
                        {
                            returnDate = GetField(result, "turn Date", 0);
                        }
                        var dob = GetField(result, "Date of Birth", 0);
                        outputRecord.Name = name.Trim();
                        outputRecord.DestinationCity = city.Trim();
                        outputRecord.DepartureDate = depDate.Trim();
                        outputRecord.ReturnDate = returnDate.Trim();
                        outputRecord.DateofBirth = dob.Trim();

                        return outputRecord;
                    }
                    else
                    {
                        throw new SystemException(response.StatusCode.ToString() + ": " + response.ToString() + "\n " + responseBody);
                    }
                    
                }
                
            }
            return outputRecord;



        }

        private static string DecodeBase64String(string encodedString)
        {
            var encodedStringWithoutTrailingCharacter = encodedString.Substring(0, encodedString.Length - 1);
            var encodedBytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(encodedStringWithoutTrailingCharacter);
            return System.Text.Encoding.UTF8.GetString(encodedBytes);
        }
        /// <summary>
        /// Searches for a field in a given page and returns the concatenated results.
        /// </summary>
        /// <param name="response">the responsed from the forms recognizer service.</param>
        /// <param name="fieldName">The field to search for</param>
        /// <param name="pageNumber">The page where the field should appear</param>
        /// <returns></returns>
        private static string GetField(FormsRecognizerResponse response, string fieldName, int pageNumber)
        {
            // Find the Address in Page 0
            if (response.pages != null)
            {
                //Assume that a given field is in the first page.
                if (response.pages[pageNumber] != null)
                {
                    foreach (var pair in response.pages[pageNumber].KeyValuePairs)
                    {
                        foreach (var key in pair.Key)
                        {
                            /// You may want to have a different comparer here 
                            /// depending on your needs.
                            if (key.Text.Contains(fieldName))
                            {
                                // then concatenate the result;
                                System.Text.StringBuilder sb = new StringBuilder();
                                foreach (var value in pair.Value)
                                {
                                    sb.Append(value.Text);
                                    // You could replace this for a newline depending on your scenario.
                                    sb.Append(" ");
                                }

                                return sb.ToString();
                            }
                        }
                    }
                }
            }

            // Could not find it in that page.
            return null;
        }

    }
}
