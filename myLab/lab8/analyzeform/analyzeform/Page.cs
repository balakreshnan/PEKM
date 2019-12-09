using System;
using System.Text;

namespace analyzeform
{
    public class Page
    {
        public int Number { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int? ClusterId { get; set; }

        public KeyValuePair[] KeyValuePairs { get; set; }
    }
}
