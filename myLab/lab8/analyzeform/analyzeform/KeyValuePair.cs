using System;
using System.Collections.Generic;
using System.Text;

namespace analyzeform
{
    public class KeyValuePair
    {
        public BoundedElement[] Key { get; set; }
        public BoundedElement[] Value { get; set; }
    }

    public class BoundedElement
    {
        public string Text { get; set; }
        public double[] BoundingBox { get; set; }

        public double Confidence { get; set; }
    }

}
