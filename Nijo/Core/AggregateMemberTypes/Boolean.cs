using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Boolean : CategorizeType {
        public override string GetCSharpTypeName() => "bool";
        public override string GetTypeScriptTypeName() => "boolean";
        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetGridCellValueFormatter() => "({ value }) => (value === undefined ? '' : (value ? '○' : '-'))";

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent {
                Name = e.Type == GetReactComponentArgs.E_Type.InDataGrid
                    ? "Input.BooleanComboBox"
                    : "Input.CheckBox",
            };
        }
    }
}
