using System;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;

namespace lab9prj
{
    public partial class insurance
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string id { get; set; }

        [IsSearchable, IsSortable]
        public string name { get; set; }

        [IsSearchable]
        public string destinationCity { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string departureDate { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string returnDate { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string dateofBirth { get; set; }

 


    }
}
