using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace lab9prj
{
    class Program
    {
        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            string indexName = configuration["SearchIndexName"];

            //Console.WriteLine("{0}", "Deleting index...\n");
            //DeleteIndexIfExists(indexName, serviceClient);

            //Console.WriteLine("{0}", "Creating index...\n");
            //CreateIndex(indexName, serviceClient);

            // Uncomment next 3 lines in "2 - Load documents"
            // ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);
            // Console.WriteLine("{0}", "Uploading documents...\n");
            // UploadDocuments(indexClient);

            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

            // Uncomment next 2 lines in "3 - Search an index"
            Console.WriteLine("{0}", "Searching index...\n");
             RunQueries(indexClient);

            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
            Console.ReadKey();

        }

        private static void WriteDocuments(DocumentSearchResult<insurance> searchResults)
        {
            foreach (SearchResult<insurance> result in searchResults.Results)
            {
                Console.WriteLine(result.Document.id);
                Console.WriteLine(result.Document.name);
                Console.WriteLine(result.Document.destinationCity);
                Console.WriteLine(result.Document.departureDate);
                Console.WriteLine(result.Document.returnDate);
                Console.WriteLine(result.Document.dateofBirth);
            }

            Console.WriteLine();
        }

        private static async Task<string> RunQueries(ISearchIndexClient indexClient)
        {
            SearchParameters parameters;
            DocumentSearchResult<insurance> results;
            string resultstr = string.Empty;

            // Query 1 
            Console.WriteLine("Query 1: Search for term 'london' with no result trimming");
            parameters = new SearchParameters();
            results = indexClient.Documents.Search<insurance>("london", parameters);
            WriteDocuments(results);

            // Update port # in the following line.
            client.BaseAddress = new Uri("http://65.52.6.162:80/api/v1/service/lab9endpoint/score");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("text/plain"));

            foreach (SearchResult<insurance> result in results.Results)
            {
                Console.WriteLine(result.Document.id);
                Console.WriteLine(result.Document.name);
                Console.WriteLine(result.Document.destinationCity);
                Console.WriteLine(result.Document.departureDate);
                Console.WriteLine(result.Document.returnDate);
                Console.WriteLine(result.Document.dateofBirth);

                claims sendclaim = new claims();
                sendclaim.AgeOnDeparture = Math.Round(DateTime.Parse(result.Document.departureDate).Subtract(DateTime.Parse(result.Document.dateofBirth)).TotalDays / 365,0).ToString();
                sendclaim.DepartureMonth = DateTime.Parse(result.Document.departureDate).Month.ToString();
                sendclaim.Destination = result.Document.destinationCity;
                sendclaim.DurationOfStay_Days = DateTime.Parse(result.Document.returnDate).Subtract(DateTime.Parse(result.Document.departureDate)).TotalDays.ToString();



                //HttpResponseMessage response = await client.PostAsync("http://65.52.6.162:80/api/v1/service/lab9endpoint/score", sendclaim);
                //if (response.IsSuccessStatusCode)
                //{
                //    resultstr = await response.Content.ReadAsAsync<string>();
                //}

                string jsonstr = "{ \"data\" : [ ";
                jsonstr += JsonConvert.SerializeObject(sendclaim);
                jsonstr += "] }";

                //var content = new StringContent(JsonConvert.SerializeObject(sendclaim), Encoding.UTF8, "application/json");

                var content = new StringContent(jsonstr, Encoding.UTF8, "application/json");

                //var content = new StringContent(jsonstr);
                //var content = new StringContent(jsonstr, Encoding.UTF8, "text/plain");

                Console.WriteLine("JSON: " + jsonstr);

                //var response = await client.PostAsync("http://65.52.6.162:80/api/v1/service/lab9endpoint/score", new StringContent(jsonstr));
                //HttpResponseMessage response = await client.PostAsync(client.BaseAddress, content);

                //response.EnsureSuccessStatusCode();
                //string content1 = await response.Content.ReadAsStringAsync();

                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    //var responseObj = JsonConvert.DeserializeObject<claimresult>(data);
                    //.Scoredprobability = responseObj.Results["output1"][0].items["output1Item"]["Scored Probabilities"];
                    //var routes_list = JsonConvert.DeserializeObject(data);
                    dynamic item = JsonConvert.DeserializeObject<object>(data);
                    //string test = item.result;

                    Console.WriteLine("output:" + data);
                    //Console.WriteLine(" Output Result: " + responseObj.result[0].ToString());
                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));
                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());
                }

                Console.ReadKey();
                //return await Task.Run(() => JsonObject.Parse(content));

            }

            //// Query 2
            //Console.WriteLine("Query 2: Search on the term 'Atlanta', with trimming");
            //Console.WriteLine("Returning only these fields: HotelName, Tags, Address:\n");
            //parameters =
            //    new SearchParameters()
            //    {
            //        Select = new[] { "name", "destinationCity", "departureDate", "returnDate", "dateofBirth" },
            //    };
            //results = indexClient.Documents.Search<insurance>("Las Vegas", parameters);
            //WriteDocuments(results);

            //// Query 3
            //Console.WriteLine("Query 3: Search for the terms 'restaurant' and 'wifi'");
            //Console.WriteLine("Return only these fields: HotelName, Description, and Tags:\n");
            //parameters =
            //    new SearchParameters()
            //    {
            //        Select = new[] { "name", "destinationCity", "departureDate", "returnDate", "dateofBirth" }
            //    };
            //results = indexClient.Documents.Search<insurance>("restaurant, wifi", parameters);
            //WriteDocuments(results);

            //// Query 4 -filtered query
            //Console.WriteLine("Query 4: Filter on ratings greater than 4");
            //Console.WriteLine("Returning only these fields: HotelName, Rating:\n");
            //parameters =
            //    new SearchParameters()
            //    {
            //       // Filter = "Rating gt 4",
            //        Select = new[] { "name", "destinationCity" }
            //    };
            //results = indexClient.Documents.Search<insurance>("*", parameters);
            //WriteDocuments(results);

            //// Query 5 - top 2 results
            //Console.WriteLine("Query 5: Search on term 'boutique'");
            //Console.WriteLine("Sort by rating in descending order, taking the top two results");
            //Console.WriteLine("Returning only these fields: HotelId, HotelName, Category, Rating:\n");
            //parameters =
            //    new SearchParameters()
            //    {
            //        OrderBy = new[] { "returnDate" },
            //        Select = new[] { "name", "destinationCity", "departureDate", "returnDate", "dateofBirth" },
            //        Top = 2
            //    };
            //results = indexClient.Documents.Search<insurance>("las vegas", parameters);
            //WriteDocuments(results);
            return resultstr;

        }

        // Create the search service client
        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
        }

        // Delete an existing index to reuse its name
        private static void DeleteIndexIfExists(string indexName, SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists(indexName))
            {
                serviceClient.Indexes.Delete(indexName);
            }
        }

        // Create an index whose fields correspond to the properties of the Hotel class.
        // The Address property of Hotel will be modeled as a complex field.
        // The properties of the Address class in turn correspond to sub-fields of the Address complex field.
        // The fields of the index are defined by calling the FieldBuilder.BuildForType() method.
        //private static void CreateIndex(string indexName, SearchServiceClient serviceClient)
        //{
        //    var definition = new Index()
        //    {
        //        Name = indexName,
        //        Fields = FieldBuilder.BuildForType<insurance>()
        //    };

        //    serviceClient.Indexes.Create(definition);
        //}


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
