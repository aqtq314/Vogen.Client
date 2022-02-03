using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public static class Utils
    {
        public static T GetOrDefault<T>(this FSharpOption<T> optionValue, T defaultValue) =>
            optionValue == null ? defaultValue : optionValue.Value;
    }
}
