using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core.AggregateMemberTypes {
    internal class YearMonth : SchalarType<DateTime> {
        public override string GetCSharpTypeName() => "int";
        public override string GetTypeScriptTypeName() => "number";
        public override string RenderUI(IGuiFormRenderer ui) => ui.DateTime(IGuiFormRenderer.E_DateType.YearMonth);
        public override string GetGridCellEditorName() => "Input.YearMonth";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string>();
    }
}
