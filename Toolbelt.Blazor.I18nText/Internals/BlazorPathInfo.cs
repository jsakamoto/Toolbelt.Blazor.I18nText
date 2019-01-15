using System;

namespace Toolbelt.Blazor.I18nText.Internals
{
    internal class BlazorPathInfo
    {
        public readonly Uri WebRootUri;

        public readonly Uri DistUri;

        public BlazorPathInfo(Uri webRoot, Uri dist)
        {
            WebRootUri = webRoot;
            DistUri = dist;
        }
    }
}
