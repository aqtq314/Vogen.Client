using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Vogen.Client.Converters
{
    public static class XamlStringifiers
    {
        public static IValueConverter TimeSignatureToString { get; } = ValueConverter.Create(
          (TimeSignature timgSig) => timgSig.ToString(),
          (string stringValue) => TimeSignature.Parse(stringValue));
    }
}
