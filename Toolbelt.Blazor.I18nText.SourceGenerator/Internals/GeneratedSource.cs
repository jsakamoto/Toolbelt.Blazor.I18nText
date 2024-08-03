namespace Toolbelt.Blazor.I18nText.SourceGenerator.Internals
{
    public readonly struct GeneratedSource
    {
        public readonly string HintName;
        public readonly string SourceCode;

        public GeneratedSource(string hintName, string sourceCode)
        {
            this.HintName = hintName;
            this.SourceCode = sourceCode;
        }
    }
}
