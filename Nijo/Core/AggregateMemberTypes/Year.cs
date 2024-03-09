using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Year : SchalarType<int> {
        public override string GetCSharpTypeName() => "int";
        public override string GetTypeScriptTypeName() => "number";
        public override string RenderUI(IGuiFormRenderer ui) => ui.DateTime(IGuiFormRenderer.E_DateType.Year);
        public override string GetGridCellEditorName() => "Input.Num";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent { Name = "Input.Num" };
        }
    }
}
