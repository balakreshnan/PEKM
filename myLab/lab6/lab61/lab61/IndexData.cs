using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace lab61
{
    [SerializePropertyNamesAsCamelCase]
    public partial class IndexData
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string Id { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        public string Url { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public string File_name { get; set; }

        [IsSearchable]
        public string Content { get; set; }

        [IsFilterable, IsSortable]
        public int Size { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public DateTime Last_modified { get; set; }

        [IsSearchable]
        public string[] keyPhrases { get; set; }

        [IsSortable, IsFacetable, IsFilterable]
        public Double Sentiment { get; set; }

        [IsSearchable, IsFilterable]
        public string[] Locations { get; set; }

        [IsSearchable, IsFilterable]
        public string[] People { get; set; }

        [IsSearchable, IsFilterable]
        public string[] Links { get; set; }

        [IsSearchable, IsFilterable]
        public string[] Entities { get; set; }

        [IsSearchable, IsFilterable]
        public string[] Image_descriptions { get; set; }

        [IsSearchable, IsFilterable]
        public string[] Image_text { get; set; }

        [IsSearchable]
        public string Merged_text { get; set; }

        [IsSearchable, IsFilterable]
        public string[] Top_words { get; set; }
    }
}
