using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace Toolbelt.Blazor.I18nText.CompileTask
{
    /// <summary>
    /// Repairs <c>StaticWebAssetEndpoint</c> items that have become inconsistent due to the execution of the <c>ComputeReferencedProjectsPublishAssets</c> target.
    /// </summary>
    /// <remarks>
    /// Specifically, for each <c>StaticWebAsset</c>, it verifies whether there is a <c>StaticWebAssetEndpoint</c> with an <c>AssetFile</c> metadata value matching its <c>ItemSpec</c>.<br/>
    /// If not found, it retrieves them from the corresponding <c>StaticWebAssetEndpoint</c> those saved before the execution of the <c>ComputeReferencedProjectsPublishAssets</c> target.<br/>
    /// Finally, it returns a complete set of <c>StaticWebAssetEndpoint</c> items for all <c>StaticWebAsset</c> items via the output parameter.
    /// </remarks>
    public class RecoverStaticWebAssetEndpoints : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Gets or sets the <c>StaticWebAsset</c> items at the time this task is executed.
        /// </summary>
        [Required]
        public ITaskItem[] StaticWebAssets { get; set; }

        /// <summary>
        /// Gets or sets the <c>StaticWebAssetEndpoint</c> items at the time this task is executed.
        /// </summary>
        [Required]
        public ITaskItem[] StaticWebAssetEndpoints { get; set; }

        /// <summary>
        /// Gets or sets the <c>StaticWebAssetEndpoint</c> items that were saved before the execution of the <c>ComputeReferencedProjectsPublishAssets</c> target.
        /// </summary>
        [Required]
        public ITaskItem[] SavedStaticWebAssetEndpoints { get; set; }

        /// <summary>
        /// Gets or sets the recovered complete set of <c>StaticWebAssetEndpoint</c> items for all <c>StaticWebAsset</c> items.
        /// </summary>
        [Output]
        public ITaskItem[] RecoveredStaticWebAssetEndpoints { get; set; }

        public override bool Execute()
        {
            // Create a dictionary to quickly look up the corresponding endpoints from 'AssetFile' metadata values
            // (Since multiple endpoints may share the same AssetFile value, use a dictionary where the key is AssetFile and the value is an array of endpoints.)
            var assetFileToEndpoints = this.StaticWebAssetEndpoints
                .GroupBy(endpoint => endpoint.GetMetadata("AssetFile"))
                .ToDictionary(g => g.Key, g => g.ToArray());

            // Prepare a similar dictionary for the saved endpoints
            var assetFileToSavedEndpoints = this.SavedStaticWebAssetEndpoints
                .GroupBy(endpoint => endpoint.GetMetadata("AssetFile"))
                .ToDictionary(g => g.Key, g => g.ToArray());

            // Retrieve the 'StaticWebAssetEndpoint' whose 'AssetFile' metadata matches each 'StaticWebAsset's 'ItemSpec'
            var recoveredEndpoints = new List<ITaskItem>();
            foreach (var staticWebAsset in this.StaticWebAssets)
            {
                // First, try to find matching endpoints from the current StaticWebAssetEndpoints.
                if (assetFileToEndpoints.TryGetValue(staticWebAsset.ItemSpec, out var endpoints))
                {
                    recoveredEndpoints.AddRange(endpoints);
                }
                // If not found, attempt to retrieve endpoints from the 'StaticWebAssetEndpoint' that were saved before the ComputeReferencedProjectsPublishAssets target execution.
                else if (assetFileToSavedEndpoints.TryGetValue(staticWebAsset.ItemSpec, out var savedEndpoints))
                {
                    recoveredEndpoints.AddRange(savedEndpoints);
                }
            }

            this.RecoveredStaticWebAssetEndpoints = recoveredEndpoints
                .OrderBy(item => item.ItemSpec)
                .ThenBy(item => item.GetMetadata("AssetFile"))
                .ToArray();
            return true;
        }
    }
}

