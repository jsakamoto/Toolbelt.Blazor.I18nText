using System;
using System.Collections.Generic;

namespace Toolbelt.Blazor.I18nText.Internals;

internal class WeakRefCollection<T> where T : class
{
    private readonly List<WeakReference<T>> Collection = new();

    public void Add(T element)
    {
        lock (this.Collection)
        {
            this.SweepGarbageCollectedComponents();

            if (!this.Collection.Exists(cref => cref.TryGetTarget(out var c) && c == element))
                this.Collection.Add(new WeakReference<T>(element));
        }
    }

    public void ForEach(Action<T> action)
    {
        lock (this.Collection)
        {
            this.SweepGarbageCollectedComponents();

            foreach (var cref in this.Collection)
            {
                if (cref.TryGetTarget(out var element))
                {
                    action(element);
                }
            }
        }
    }

    private void SweepGarbageCollectedComponents()
    {
        lock (this.Collection)
        {
            // DEBUG: var beforeCount = this.Components.Count;
            for (var i = this.Collection.Count - 1; i >= 0; i--)
            {
                if (!this.Collection[i].TryGetTarget(out var _)) this.Collection.RemoveAt(i);
            }
            // DEBUG: var afterCount = this.Components.Count;
            // DEBUG: Console.WriteLine($"SweepGarbageCollectedComponents - {(beforeCount - afterCount)} objects are sweeped. ({this.Components.Count} objects are stay.)");
        }
    }
}
