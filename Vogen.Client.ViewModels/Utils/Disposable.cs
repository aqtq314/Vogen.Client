using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Utils
{
    public class Disposable : IDisposable
    {
        public Action? Disposer { get; init; }

        private Disposable(Action? disposer)
        {
            Disposer = disposer;
        }

        public void Dispose()
        {
            Disposer?.Invoke();
        }

        public static IDisposable Empty => new Disposable(null);

        public static IDisposable Create(Action disposer) => new Disposable(disposer);

        public static IDisposable CreateOnce(Action baseDisposer)
        {
            var disposer = new Lazy<Unit?>(() =>
            {
                baseDisposer();
                return null;
            });

            return new Disposable(() =>
            {
                var _ = disposer.Value;
            });
        }
    }
}
