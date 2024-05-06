using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    internal class DataTableColumn {

        internal static IEnumerable<DataTableColumn> FromMembers(
            string rowAccessor,
            GraphNode<Aggregate> dataTableOwner,
            bool readOnly) {

            // ----------------------------------------------------
            // AggregateMember列
            var colIndex = 0;
            DataTableColumn ToDataTableColumn(AggregateMember.AggregateMemberBase member) {
                var vm = member as AggregateMember.ValueMember;
                var refMember = member as AggregateMember.Ref;

                var memberPath = member.GetFullPathAsSingleViewDataClass(since: dataTableOwner);

                // 非編集時のセル表示文字列
                string? formatted = null;
                if (vm != null) {
                    var component = vm.Options.MemberType.GetReactComponent(new() {
                        Type = GetReactComponentArgs.E_Type.InDataGrid,
                    });
                    if (component.GridCellFormatStatement != null) {
                        formatted = component.GridCellFormatStatement("value", "formatted");
                    }
                }

                var cell = $$"""
                    cellProps => {
                      const value = cellProps.row.original.{{rowAccessor}}.{{memberPath.Join("?.")}}
                      {{If(formatted != null, () => WithIndent(formatted!, "  "))}}
                      return (
                        <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
                          {{{(formatted == null ? "value" : "formatted")}}}
                          &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
                        </span>
                      )
                    }
                    """;

                string? cellEditor;
                if (readOnly) {
                    cellEditor = null;
                } else if (member is AggregateMember.ValueMember vm2) {
                    var editor = vm2.Options.MemberType.GetReactComponent(new() {
                        Type = GetReactComponentArgs.E_Type.InDataGrid,
                    });
                    cellEditor = $"(props, ref) => <{editor.Name} ref={{ref}} {{...props}}{string.Concat(editor.GetPropsStatement())} />";

                } else if (member is AggregateMember.Ref rm2) {
                    var combobox = new ComboBox(rm2.MemberAggregate);
                    cellEditor = $"(props, ref) => <Input.{combobox.ComponentName} ref={{ref}} {{...props}} />";

                } else {
                    throw new InvalidProgramException();
                }

                var getValue = $"data => data.{rowAccessor}.{memberPath.Join("?.")}";

                string? setValue;
                if (readOnly) {
                    setValue = null;
                } else if (member.DeclaringAggregate == dataTableOwner) {
                    setValue = $$"""
                        (row, value) => row.{{rowAccessor}}.{{memberPath.Join(".")}} = value
                        """;
                } else {
                    var ownerPath = member.Owner.GetFullPathAsSingleViewDataClass(since: dataTableOwner);
                    var rootAggPath = member.Owner.GetRoot().GetFullPathAsSingleViewDataClass(since: dataTableOwner);
                    setValue = $$"""
                        (row, value) => {
                          if (row.{{rowAccessor}}.{{ownerPath.Join("?.")}}) {
                            row.{{rowAccessor}}.{{memberPath.Join(".")}} = value
                            row.{{rowAccessor}}{{rootAggPath.Select(x => $".{x}").Join("")}}.{{DisplayDataClass.WILL_BE_CHANGED}} = true
                          }
                        }
                        """;
                }

                var hidden = vm?.Options.InvisibleInGui == true
                    ? true
                    : (bool?)null;

                var headerGroupName = member.Owner == dataTableOwner
                    ? null
                    : member.Owner.Item.DisplayName;

                colIndex++;

                return new DataTableColumn {
                    Id = $"col{colIndex}",
                    Header = member.MemberName,
                    Cell = cell,
                    CellEditor = cellEditor,
                    GetValue = getValue,
                    SetValue = setValue,
                    Hidden = hidden,
                    HeaderGroupName = headerGroupName,
                };
            }

            // ----------------------------------------------------
            // テーブル中の被参照集約の列のインスタンスを追加または削除するボタン
            DataTableColumn RefFromButtonColumn(DisplayDataClass.RelationProp refFrom) {
                var tableArrayRegisterName = dataTableOwner.GetRHFRegisterName();
                var pageRoot = new DisplayDataClass(dataTableOwner.GetEntry().As<Aggregate>());
                var refFromDisplayData = new DisplayDataClass(refFrom.MainAggregate);
                var value = refFrom.MainAggregate.Item.ClassName;
                var registerName = refFrom.MainAggregate.GetRHFRegisterName(["row.index"]);

                return new DataTableColumn {
                    Id = $"ref-from-{refFrom.PropName}",
                    Header = string.Empty,
                    HeaderGroupName = refFrom.MainAggregate.Item.ClassName,
                    Cell = $$"""
                        ({ row }) => {
                          const {{value}} = row.original.{{rowAccessor}}.{{refFrom.MainAggregate.GetFullPathAsSingleViewDataClass(since: dataTableOwner).Join("?.")}}
                          const { setValue } = Util.useFormContextEx<AggregateType.{{pageRoot.TsTypeName}}>()

                          const create{{value}} = useCallback(() => {
                            setValue(`{{refFrom.MainAggregate.GetRHFRegisterName(["row.index"]).Join(".")}}`, {{WithIndent(refFromDisplayData.RenderNewObjectLiteral(), "    ")}})
                          }, [setValue, row.index])

                          const delete{{value}} = useCallback(() => {
                            setValue(`{{refFrom.MainAggregate.GetRHFRegisterName(["row.index"]).Join(".")}}.{{DisplayDataClass.WILL_BE_DELETED}}`, true)
                          }, [setValue, row.index])

                          return <>
                            {({{value}} === undefined || {{value}}.{{DisplayDataClass.WILL_BE_DELETED}}) && (
                              <Input.Button icon={PlusIcon} onClick={create{{value}}}>作成</Input.Button>
                            )}
                            {{{value}} !== undefined && (
                              <Input.Button icon={XMarkIcon} onClick={delete{{value}}}>削除</Input.Button>
                            )}
                          </>
                        }
                        """,
                };
            }

            // ----------------------------------------------------

            // グリッドに表示するメンバーを列挙
            IEnumerable<DataTableColumn> Collect(DisplayDataClass dataClass) {
                foreach (var prop in dataClass.GetOwnProps()) {
                    if (prop.Member is AggregateMember.ValueMember vm) {
                        if (vm.DeclaringAggregate != dataClass.MainAggregate) continue;
                        if (vm.Options.InvisibleInGui) continue;
                        yield return ToDataTableColumn(prop.Member);

                    } else if (prop.Member is AggregateMember.Ref @ref) {
                        if (@ref.MemberAggregate.IsSingleRefKeyOf(@ref.Owner)) continue;
                        yield return ToDataTableColumn(prop.Member);
                    }
                }

                foreach (var prop in dataClass.GetChildProps()) {

                    // ChildrenやVariationのメンバーを列挙していないのはグリッド上で表現できないため
                    if (prop.MemberInfo is AggregateMember.Children) continue;
                    if (prop.MemberInfo is AggregateMember.VariationItem) continue;

                    foreach (var reucusive in Collect(new DisplayDataClass(prop.MainAggregate))) {
                        yield return reucusive;
                    }
                }

                foreach (var prop in dataClass.GetRefFromProps()) {
                    yield return RefFromButtonColumn(prop);
                    foreach (var recursive in Collect(new DisplayDataClass(prop.MainAggregate))) {
                        yield return recursive;
                    }
                }
            }

            var root = new DisplayDataClass(dataTableOwner);

            foreach (var column in Collect(root)) {
                yield return column;
            }
        }

        // react table のAPI
        internal required string Id { get; init; }
        internal required string Header { get; init; }
        internal required string Cell { get; init; }
        internal int? Size { get; init; }
        internal bool? EnableResizing { get; init; }
        /// <summary>accessorFnにマッピングされる</summary>
        internal string? GetValue { get; init; }

        // 独自定義
        internal string? CellEditor { get; init; }
        internal string? SetValue { get; init; }
        internal bool? Hidden { get; init; }
        internal string? HeaderGroupName { get; init; }

        internal string Render() {
            return $$"""
                {
                  id: '{{Id}}',
                  header: '{{Header}}',
                  cell: {{WithIndent(Cell, "  ")}},
                {{If(Size != null, () => $$"""
                  size: {{Size}},
                """)}}
                {{If(EnableResizing != null, () => $$"""
                  enableResizing: {{(EnableResizing!.Value ? "true" : "false")}},
                """)}}
                {{If(GetValue != null, () => $$"""
                  accessorFn: {{GetValue}},
                """)}}
                {{If(SetValue != null, () => $$"""
                  setValue: {{WithIndent(SetValue!, "  ")}},
                """)}}
                {{If(CellEditor != null, () => $$"""
                  cellEditor: {{WithIndent(CellEditor!, "  ")}},
                """)}}
                {{If(Hidden != null, () => $$"""
                  hidden: {{(Hidden!.Value ? "true" : "false")}},
                """)}}
                {{If(HeaderGroupName != null, () => $$"""
                  headerGroupName: '{{HeaderGroupName}}',
                """)}}
                },
                """;
        }
    }
}
