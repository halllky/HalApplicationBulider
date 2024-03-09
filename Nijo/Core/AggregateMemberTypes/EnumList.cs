using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    public class EnumList : CategorizeType {
        public EnumList(EnumDefinition definition) {
            Definition = definition;
        }
        public EnumDefinition Definition { get; }

        public override SearchBehavior SearchBehavior => SearchBehavior.Strict;
        public override string GetCSharpTypeName() => Definition.Name;
        public override string GetTypeScriptTypeName() {
            return Definition.Items.Select(x => $"'{x.PhysicalName}'").Join(" | ");
        }
        public override string RenderUI(IGuiFormRenderer ui) {
            var options = Definition.Items.ToDictionary(
                x => x.PhysicalName,
                x => x.DisplayName ?? x.PhysicalName);
            return ui.Selection(options);
        }
        public override string GetGridCellEditorName() => "Input.ComboBox";
        public override IReadOnlyDictionary<string, string> GetGridCellEditorParams() => new Dictionary<string, string> {
            { "options", $"[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]" },
            { "keySelector", "(item: string) => item" },
            { "textSelector", "(item: string) => item" },
        };

        public override ReactInputComponent GetReactComponent(GetReactComponentArgs e) {
            return new ReactInputComponent {
                Name = e.Type == GetReactComponentArgs.E_Type.InDetailView
                    ? "Input.SelectionEmitsKey"
                    : "Input.ComboBox",
                Props = new Dictionary<string, string> {
                    { "options", $"[{Definition.Items.Select(x => $"'{x.PhysicalName}' as const").Join(", ")}]" },
                    { "keySelector", "item => item" },
                    { "textSelector", "item => item" },
                },
            };
        }
    }
}
