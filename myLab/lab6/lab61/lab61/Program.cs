using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace lab61
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            ISearchIndexClient indexClientForQueries = CreateSearchIndexClient(configuration);

            Console.WriteLine("Running Queries");
            RunQueries(indexClientForQueries);
        }

        private static SearchIndexClient CreateSearchIndexClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string queryApiKey = configuration["SearchServiceQueryApiKey"];

            SearchIndexClient indexClient = new SearchIndexClient(searchServiceName, configuration["IndexName"], new SearchCredentials(queryApiKey));
            return indexClient;
        }

        private static void RunQueries(ISearchIndexClient indexClient)
        {
            SearchParameters parameters;
            DocumentSearchResult<IndexData> results;

            //Return search results based on synonyms - for example, a search for "Britain" should return documents that include "United Kingdom" or "UK".
            Console.WriteLine("**************************************************************************");
            Console.WriteLine("Search for Britain \n\n");

            parameters =
                new SearchParameters()
                {
                    Select = new[] { "content", "locations", "file_name" }
                };

            results = indexClient.Documents.Search<IndexData>("Britain", parameters);
            foreach (SearchResult<IndexData> result in results.Results)
            {
                Console.WriteLine("File Name: {0}", result.Document.File_name);
                Console.WriteLine("Locations: {0}", string.Join(',', result.Document.Locations));
                Console.WriteLine("Content: {0}\n\n", result.Document.Content);

            }
            Console.WriteLine();

            //displays suggestions and autocomplete options for partial user input based on the *Location* field. 
            //For example, typing "San" should produce suggestions for "**San** Francisco" and "**San** Diego"
            Console.WriteLine("**************************************************************************");
            Console.WriteLine("Suggest something for San \n\n");

            SuggestParameters par = new SuggestParameters()
            {
                Top = 15
            };


            AutocompleteParameters ap = new AutocompleteParameters()
            {
                AutocompleteMode = AutocompleteMode.TwoTerms
            };

            var autoComplete = indexClient.Documents.Autocomplete("San", "sg", ap);
            Console.WriteLine("Autocomplete options:");
            foreach (var result in autoComplete.Results)
            {
                Console.WriteLine(result.Text);
            }

            //var suggests = indexClient.Documents.Suggest<IndexData>("uk", "sg", par);

            //Console.WriteLine("\nSuggestion options:");

            //foreach (var result in suggests.Results)
            //{
            //    Console.WriteLine("File Name:{0}", result.Document.File_name);
            //    Console.WriteLine("Locations:{0}\n", string.Join(",", result.Document.Locations));
            //}

            //Console.WriteLine();

            //Apply a scoring profile to increase the search score of results based on term inclusion in key phrases, 
            //document size, and last modified date. For example, searching for "quiet" should boost results where the word "quiet" is included in the key phrases field.
            Console.WriteLine("**************************************************************************");
            Console.WriteLine("Search for quiet with boosting \n\n");

            parameters =
                new SearchParameters()
                {
                    Select = new[] { "size", "keyPhrases", "file_name" },
                    ScoringProfile = "geo",
                    QueryType = QueryType.Full,
                    SearchMode = SearchMode.All,
                    Filter = "search.ismatch('reviews', 'url')"
                };

            results = indexClient.Documents.Search<IndexData>("quiet", parameters);
            foreach (SearchResult<IndexData> result in results.Results)
            {
                Console.WriteLine("Score: {0}", result.Score);
                Console.WriteLine("File Name: {0}", result.Document.File_name);
                Console.WriteLine("Key Phrases: {0}", string.Join(',', result.Document.keyPhrases));
                Console.WriteLine("Size: {0}", result.Document.Size);
                //Console.WriteLine("Last Modified: {0}\n\n", result.Document.Last_modified);

            }
            Console.WriteLine();
        }
    }
}
