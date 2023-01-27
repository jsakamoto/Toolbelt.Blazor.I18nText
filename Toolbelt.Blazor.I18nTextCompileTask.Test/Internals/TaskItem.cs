using System.Collections;
using Microsoft.Build.Framework;

namespace Toolbelt.Blazor.I18nText.Test.Internals;
internal class TaskItem : ITaskItem
{
    public TaskItem(string itemSpec) => this.ItemSpec = itemSpec;
    public string ItemSpec { get; set; } = "";
    public ICollection MetadataNames => default!;
    public int MetadataCount => 0;
    public IDictionary CloneCustomMetadata() => throw new NotImplementedException();
    public void CopyMetadataTo(ITaskItem destinationItem) => throw new NotImplementedException();
    public string GetMetadata(string metadataName) => metadataName switch { "Encoding" => "", _ => throw new NotImplementedException() };
    public void RemoveMetadata(string metadataName) => throw new NotImplementedException();
    public void SetMetadata(string metadataName, string metadataValue) => throw new NotImplementedException();
}
