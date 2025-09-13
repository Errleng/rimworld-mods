using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rimfo
{
    class SemiNumericComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var regex = new Regex(@"^([-+]?[0-9]*\.?[0-9]+)");

            // run the regex on both strings
            var xRegexResult = regex.Match(x);
            var yRegexResult = regex.Match(y);

            // check if they are both numbers
            if (xRegexResult.Success && yRegexResult.Success)
            {
                return float.Parse(xRegexResult.Groups[1].Value).CompareTo(float.Parse(yRegexResult.Groups[1].Value));
            }

            // otherwise return as string comparison
            return x.CompareTo(y);
        }
    }
}