using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Utils
{
    public static class ExtensionMethods
    {
        public static T GetOrDefault<T>(this FSharpOption<T> optionValue, T defaultValue) =>
            optionValue == null ? defaultValue : optionValue.Value;

        public static byte[] ReadAllBytes(this Stream stream)
        {
            using var byteStream = new MemoryStream();
            stream.CopyTo(byteStream);
            return byteStream.ToArray();
        }
    }
}
