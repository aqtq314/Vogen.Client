using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vogen.Synth;

namespace Vogen.Client.ViewModels
{
    public static class DesignerModel
    {
        public static ProgramViewModel Program { get; private set; }

        static DesignerModel()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"Vogen.Client.ViewModels.testComp.vog") ??
                throw new KeyNotFoundException();
            var vog = VogPackage.read(stream, ".");

            Program = new ProgramViewModel();
            Program.ImportFromVog(vog);
        }
    }
}
