using System.Collections.Generic;

namespace Destrospean.Misc
{
    public class DummyComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return 1;
        }
    }
}
