using System.Text;

namespace Toolbelt.Blazor.I18nText
{
    public class I18nTextSourceFile
    {
        public string Path { get; }

        public Encoding Encoding { get; }

        public I18nTextSourceFile(string path, Encoding encoding)
        {
            Path = path;
            Encoding = encoding;
        }
    }
}
