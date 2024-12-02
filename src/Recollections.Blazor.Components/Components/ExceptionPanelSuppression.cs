using System;
using System.Collections.Generic;
using System.Linq;

namespace Neptuo.Recollections.Components;

public class ExceptionPanelSuppression
{
    public IDisposable Enter<T>(Func<T, bool> filter = null)
        => new Disposable(this, e => e is T ex && (filter == null || filter(ex)));

    public bool IsMatched(Exception e)
        => Current.Any(c => c.IsMatched(e));

    class Disposable : IDisposable
    {
        private readonly ExceptionPanelSuppression context;
        private readonly Func<Exception, bool> filter;

        public Disposable(ExceptionPanelSuppression context, Func<Exception, bool> filter)
        {
            this.context = context;
            this.filter = filter;
            context.Current.Add(this);
        }

        public bool IsMatched(Exception e) => filter(e);

        public void Dispose()
            => context.Current.Remove(this);
    }

    private List<Disposable> Current { get; } = new();
}