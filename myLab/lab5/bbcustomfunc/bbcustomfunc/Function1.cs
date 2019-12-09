using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;

namespace bbcustomfunc
{
    public static class Function1
    {
        #region Class used to deserialize the request
        private class InputRecord
        {
            public class InputRecordData
            {
                public string MergedText { get; set; }
            }

            public string RecordId { get; set; }
            public InputRecordData Data { get; set; }
        }

        private class WebApiRequest
        {
            public List<InputRecord> Values { get; set; }
        }
        #endregion
        private class OutputRecord
        {
            public class OutputRecordMessage
            {
                public string Message { get; set; }
            }

            public class OutputRecordData
            {
                public List<string> TopWords { get; set; }
            }
            public string recordId { get; set; }
            public OutputRecordData Data { get; set; }
            public List<OutputRecordMessage> Errors { get; set; }
            public List<OutputRecordMessage> Warnings { get; set; }
        }

        //Build a list of responses
        private class Response
        {
            public List<OutputRecord> Values { get; set; }
        }

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //string name = req.Query["name"];

            //string test_text = @"Four score and seven years ago our fathers brought forth on this continent, a new nation, conceived in Liberty, and dedicated to the proposition that all men are created equal.
            //    Now we are engaged in a great civil war, testing whether that nation, or any nation so conceived and so dedicated, can long endure. We are met on a great battlefield of that war.
            //    We have come to dedicate a portion of that field, as a final resting place for those who here gave their lives that that nation might live. It is altogether fitting and proper that we should do this.\
            //    But, in a larger sense, we can not dedicate, we can not consecrate, we can not hallow this ground. The brave men, living and dead, who struggled here, have consecrated it, far above our poor power to add or detract.\
            //    The world will little note, nor long remember what we say here, but it can never forget what they did here. It is for us the living, rather, to be dedicated here to the unfinished work which they who fought here have thus far so nobly advanced.\
            //    It is rather for us to be here dedicated to the great task remaining before us that from these honored dead we take increased devotion to that cause for which they gave the last full measure of devotion, that we here highly resolve that these dead shall not have died in vain; that this nation, under God, shall have a new birth of freedom and that government of the people, by the people, for the people, shall not perish from the earth.";

            ////word_list test_result = get_top_ten_words(test_text);///
            //word_list test_result = get_top_ten_words(name);


            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //string count = test_result.words.Count.ToString();

            //return name != null
            //    ? (ActionResult)new OkObjectResult($"{JsonConvert.SerializeObject(test_result)}")
            //    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var rawData = JsonConvert.DeserializeObject<WebApiRequest>(requestBody);
            List<InputRecord> values = rawData.Values;
            // Validation
            if (rawData.Values == null)
            {
                return new BadRequestObjectResult(" Could not find values array");
            }

            //Initialize the variables ahead of time
            Response response = new Response();
            response.Values = new List<OutputRecord>();

            foreach (var record in values)
            {
                string recordId = record.RecordId;
                string mergedText = record.Data.MergedText;
                if (recordId == null)
                {
                    return new BadRequestObjectResult("recordId cannot be null");
                }

                // Build the response
                OutputRecord responseRecord = new OutputRecord();
                responseRecord.Data = new OutputRecord.OutputRecordData();
                responseRecord.recordId = recordId;
                try
                {
                    responseRecord.Data.TopWords = get_top_ten_words(mergedText);
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

                response.Values.Add(responseRecord);
            }
            return (ActionResult)new OkObjectResult(response);
        }

        public static List<string> get_top_ten_words(string text)
        {

            if (text == null)
            {
                return null;
            }

            // convert to lowercase
            text = text.ToLowerInvariant();
            List<string> words = text.Split(' ').ToList();

            //remove any non alphabet characters
            var onlyAlphabetRegEx = new Regex(@"^[A-z]+$");
            words = words.Where(f => onlyAlphabetRegEx.IsMatch(f)).ToList();

            // Array of stop words to be ignored
            string[] stopwords = { "", "i", "me", "my", "myself", "we", "our", "ours", "ourselves", "you",
                "youre", "youve", "youll", "youd", "your", "yours", "yourself",
                "yourselves", "he", "him", "his", "himself", "she", "shes", "her",
                "hers", "herself", "it", "its", "itself", "they", "them", "thats",
                "their", "theirs", "themselves", "what", "which", "who", "whom",
                "this", "that", "thatll", "these", "those", "am", "is", "are", "was",
                "were", "be", "been", "being", "have", "has", "had", "having", "do",
                "does", "did", "doing", "a", "an", "the", "and", "but", "if", "or",
                "because", "as", "until", "while", "of", "at", "by", "for", "with",
                "about", "against", "between", "into", "through", "during", "before",
                "after", "above", "below", "to", "from", "up", "down", "in", "out",
                "on", "off", "over", "under", "again", "further", "then", "once", "here",
                "there", "when", "where", "why", "how", "all", "any", "both", "each",
                "few", "more", "most", "other", "some", "such", "no", "nor", "not",
                "only", "own", "same", "so", "than", "too", "very", "s", "t", "can",
                "will", "just", "don", "dont", "should", "shouldve", "now", "d", "ll",
                "m", "o", "re", "ve", "y", "ain", "aren", "arent", "couldn", "couldnt",
                "didn", "didnt", "doesn", "doesnt", "hadn", "hadnt", "hasn", "hasnt",
                "havent", "isn", "isnt", "ma", "mightn", "mightnt", "mustn",
                "mustnt", "needn", "neednt", "shan", "shant", "shall", "shouldn", "shouldnt", "wasn",
                "wasnt", "weren", "werent", "won", "wont", "wouldn", "wouldnt"};
            words = words.Where(x => !stopwords.Contains(x)).ToList();

            // Find distict keywords by key and count, and then order by count.
            var keywords = words.GroupBy(x => x).OrderByDescending(x => x.Count());
            var klist = keywords.ToList();
            var numofWords = 10;
            if (klist.Count < 10)
                numofWords = klist.Count;

            // Return the first 10 words
            List<string> resList = new List<string>();
            for (int i = 0; i < numofWords; i++)
            {
                resList.Add(klist[i].Key);
            }

            // Construct object for results
            List<string> json_result = resList;

            // return the results object
            return json_result;
        }

    }

    // class for results
    public class word_list
    {
        public List<string> words { get; set; }
    }

}
