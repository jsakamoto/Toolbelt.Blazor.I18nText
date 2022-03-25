using System.Text;
using NUnit.Framework;
using Toolbelt.Blazor.I18nText.Internals;

namespace Toolbelt.Blazor.I18nText.SourceGenerator.Test;

public class I18nTextTableStreamTest
{
    [Test]
    public void Read_Test()
    {
        var i18ntext = new I18nTextType();
        i18ntext.Langs.TryAdd("en", new I18nTextTable(new Dictionary<string, string>
        {
            {"Greeting", "Hello, World!" },
            {"Home", "Home" }
        }));
        i18ntext.Langs.TryAdd("fr", new I18nTextTable(new Dictionary<string, string>
        {
            {"Greeting", "Bonjour le monde!" }
        }));

        using var stream = new I18nTextTableStream(i18ntext);
        var buff = new List<byte>();
        var readBuff = new byte[3];
        for (; ; )
        {
            var cbRead = stream.Read(readBuff, 0, 3);
            if (cbRead == 0) break;
            buff.AddRange(readBuff.Take(cbRead));
        }

        Encoding.UTF8.GetString(buff.ToArray()).Is("enGreetingHello, World!HomeHomefrGreetingBonjour le monde!");
    }
}
