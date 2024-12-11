using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace numerical_methods
{
    public class Result
    {
        public string Method { get; set; }
        public int ElementCount { get; set; }
        public long Time { get; set; }
        public double ErrorPercent { get; set; }
        public Result() { }
        public Result(string method, int elementCount, long time, double errorPercent)
        {
            Method = method;
            ElementCount = elementCount;
            Time = time;
            ErrorPercent = errorPercent;
        }
    }
}
