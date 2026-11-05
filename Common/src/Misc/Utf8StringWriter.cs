using System.IO;
using System.Text;

namespace Destrospean.Misc
{
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public Utf8StringWriter(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
    }
}
