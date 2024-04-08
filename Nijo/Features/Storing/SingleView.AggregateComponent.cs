using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;
using Nijo.Parts.WebClient;

namespace Nijo.Features.Storing {

    internal class AggregateComponent {
        internal AggregateComponent(GraphNode<Aggregate> aggregate, SingleView.E_Type type) {
            if (!aggregate.IsRoot()) throw new ArgumentException("ルート集約でない場合はもう片方のコンストラクタを使用");

            _aggregate = aggregate;
            _relationToParent = null;
            _mode = type;
        }
        internal AggregateComponent(AggregateMember.RelationMember relationMember, SingleView.E_Type type) {
            _aggregate = relationMember.MemberAggregate;
            _relationToParent = relationMember;
            _mode = type;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly AggregateMember.RelationMember? _relationToParent;
        private readonly SingleView.E_Type _mode;

        internal string RenderCaller() {
            var componentName = GetComponentName();
            var args = GetArguments()
                .Select(arg => $" {arg}={{{arg}}}")
                .Join(string.Empty);

            return $"<{componentName}{args} />";
        }

        internal string RenderDeclaration() {
            var dataClass = new SingleViewDataClass(_aggregate);
            var componentName = GetComponentName();
            var args = GetArguments().ToArray();

            // useFormの型。Refの参照元のコンポーネントのレンダリングの可能性があるためGetRootではなくGetEntry
            var entryDataClass = new SingleViewDataClass(_aggregate.GetEntry().As<Aggregate>());
            var useFormType = $"AggregateType.{entryDataClass.TsTypeName}";
            var registerName = GetRegisterName();

            // この集約を参照する隣接集約 ※DataTableの列定義は当該箇所で定義している
            var relevantAggregatesCalling = _aggregate
                .GetReferedEdgesAsSingleKey()
                .SelectTextTemplate(edge =>  $$"""
                    <VForm.Container label="{{edge.Initial.Item.DisplayName}}">
                      {{WithIndent(new AggregateComponent(edge.Initial, _mode).RenderCaller(), "  ")}}
                    </VForm.Container>
                    """);

            if (_relationToParent == null) {
                // ルート集約のレンダリング
                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, watch, getValues } = Util.useFormContextEx<{{useFormType}}>()
                      const item = getValues({{registerName}})

                      return (
                        <>
                          {{WithIndent(RenderMembers(), "      ")}}
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </>
                      )
                    }
                    """;

            } else if (_relationToParent is AggregateMember.Child) {
                // Childのレンダリング
                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { register, registerEx, watch, getValues } = Util.useFormContextEx<{{useFormType}}>()
                      const item = getValues({{registerName}})

                      return (
                        <>
                          {{WithIndent(RenderMembers(), "      ")}}
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </>
                      )
                    }
                    """;

            } else if (_relationToParent is AggregateMember.VariationItem variation) {
                // Variationメンバーのレンダリング
                var switchProp = GetRegisterName(_aggregate.GetParent()!.Initial, variation.Group);

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, getValues } = Util.useFormContextEx<{{useFormType}}>()
                      const item = getValues({{registerName}})

                      const body = (
                        <>
                          {{WithIndent(RenderMembers(), "      ")}}
                          {{WithIndent(relevantAggregatesCalling, "      ")}}
                        </>
                      )

                      return watch({{switchProp}}) === '{{variation.Key}}'
                        ? (
                          <>
                            {body}
                          </>
                        ) : (
                          <div className="hidden">
                            {body}
                          </div>
                        )
                    }
                    """;

            } else if (!_aggregate.CanDisplayAllMembersAs2DGrid()) {
                // Childrenのレンダリング（子集約をもつなど表であらわせない場合）
                var loopVar = $"index_{args.Length}";

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, control } = Util.useFormContextEx<{{useFormType}}>()
                      const { fields, append, remove } = useFieldArray({
                        control,
                        name: {{registerName}},
                      })
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append(AggregateType.{{dataClass.TsInitFunctionName}}())
                        e.preventDefault()
                      }, [append])
                      const onCreate = useCallback(() => {
                        append(AggregateType.{{dataClass.TsInitFunctionName}}())
                      }, [append])
                      const onRemove = useCallback((index: number) => {
                        return (e: React.MouseEvent) => {
                          remove(index)
                          e.preventDefault()
                        }
                      }, [remove])
                    """)}}

                      return (
                        <VForm.Container labelSide={(
                          <div className="flex gap-2 justify-start">
                            <h1 className="text-base font-semibold select-none py-1">
                              {{_aggregate.GetParent()?.RelationName}}
                            </h1>
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                            <Input.Button onClick={onCreate}>追加</Input.Button>
                    """)}}
                            <div className="flex-1"></div>
                          </div>
                        )}>
                          {fields.map((item, {{loopVar}}) => (
                    {{If(_mode == SingleView.E_Type.View, () => $$"""
                            <VForm.Container key={{{loopVar}}}>
                    """).Else(() => $$"""
                            <VForm.Container key={{{loopVar}}} labelSide={(
                              <Input.IconButton
                                underline
                                icon={XMarkIcon}
                                onClick={onRemove({{loopVar}})}>
                                削除
                              </Input.IconButton>
                            )}>
                    """)}}
                              {{WithIndent(RenderMembers(), "          ")}}
                            </VForm.Container>
                          ))}
                                  {{WithIndent(relevantAggregatesCalling, "              ")}}
                        </VForm.Container>
                      )
                    }
                    """;

            } else {
                // Childrenのレンダリング（子集約をもたない場合）
                var loopVar = $"index_{args.Length}";
                var editable = _mode == SingleView.E_Type.View ? "false" : "true";

                var colDefs = DataTableColumn.FromMembers(
                        "item",
                        _aggregate,
                        _mode == SingleView.E_Type.View);

                return $$"""
                    const {{componentName}} = ({{{args.Join(", ")}} }: {
                    {{args.SelectTextTemplate(arg => $$"""
                      {{arg}}: number
                    """)}}
                    }) => {
                      const { registerEx, watch, control } = Util.useFormContextEx<{{useFormType}}>()
                      const { fields, append, remove, update } = useFieldArray({
                        control,
                        name: {{registerName}},
                      })
                      const dtRef = useRef<Layout.DataTableRef<AggregateType.{{dataClass.TsTypeName}}>>(null)

                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                      const onAdd = useCallback((e: React.MouseEvent) => {
                        append(AggregateType.{{dataClass.TsInitFunctionName}}())
                        e.preventDefault()
                      }, [append])
                      const onRemove = useCallback((e: React.MouseEvent) => {
                        const selectedRowIndexes = dtRef.current?.getSelectedIndexes() ?? []
                        for (const index of selectedRowIndexes.sort((a, b) => b - a)) remove(index)
                        e.preventDefault()
                      }, [remove])
                    """)}}

                      const options = useMemo<Layout.DataTableProps<AggregateType.{{dataClass.TsTypeName}}>>(() => ({
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                        onChangeRow: update,
                    """)}}
                        columns: [
                          {{WithIndent(colDefs.SelectTextTemplate(def => def.Render()), "      ")}}
                        ],
                      }), [])

                      return (
                        <VForm.Item wide
                          label="{{_aggregate.GetParent()?.RelationName}}"
                    {{If(_mode != SingleView.E_Type.View, () => $$"""
                          labelSide={<>
                            <Input.Button
                              icon={PlusIcon}
                              onClick={onAdd}>
                              追加
                            </Input.Button>
                            <Input.Button
                              icon={XMarkIcon}
                              onClick={onRemove}>
                              削除
                            </Input.Button>
                          </>}
                    """)}}
                          >
                          <Layout.DataTable
                            ref={dtRef}
                            data={fields}
                            {...options}
                            className="h-64 w-full"
                          />
                        </VForm.Item>
                      )
                    }
                    """;
            }
        }

        private string RenderMembers() {
            return GetMembers().SelectTextTemplate(prop => prop switch {
                AggregateMember.Schalar x => RenderProperty(x),
                AggregateMember.Ref x => RenderProperty(x),
                AggregateMember.Child x => RenderProperty(x),
                AggregateMember.VariationItem x => string.Empty, // Variationの分岐の中でレンダリングされるので // RenderProperty(x),
                AggregateMember.Variation x => RenderProperty(x),
                AggregateMember.Children x => RenderProperty(x),
                _ => throw new NotImplementedException(),
            });
        }

        private string RenderProperty(AggregateMember.Children children) {
            var childrenComponent = new AggregateComponent(children, _mode);

            return $$"""
                {{childrenComponent.RenderCaller()}}
                """;
        }

        private string RenderProperty(AggregateMember.Child child) {
            var childComponent = new AggregateComponent(child, _mode);

            return $$"""
                <VForm.Container label="{{child.MemberName}}">
                  {{childComponent.RenderCaller()}}
                </VForm.Container>
                """;
        }

        private string RenderProperty(AggregateMember.VariationItem variation) {
            var childComponent = new AggregateComponent(variation, _mode);
            return $$"""
                {{WithIndent(childComponent.RenderCaller(), "")}}
                """;
        }

        private string RenderProperty(AggregateMember.Variation variationSwitch) {
            var switchProp = GetRegisterName(variationSwitch);
            var disabled = IfReadOnly("disabled", variationSwitch);

            return $$"""
                <VForm.Container
                  labelSide={<>
                    {{variationSwitch.MemberName}}
                    <Input.Selection
                      {...registerEx({{switchProp}})}
                      options={[
                {{variationSwitch.GetGroupItems().SelectTextTemplate(variation => $$"""
                        { value: '{{variation.Key}}', text: '{{variation.MemberName}}' },
                """)}}
                      ]}
                      keySelector={item => item.value}
                      textSelector={item => item.text}
                    />
                  </>}
                >
                {{variationSwitch.GetGroupItems().SelectTextTemplate(item => $$"""
                  {{WithIndent(RenderProperty(item), "  ")}}
                """)}}
                </VForm.Container>
                """;
        }

        private string RenderProperty(AggregateMember.Ref refProperty) {
            if (_aggregate != _aggregate.GetEntry().As<Aggregate>()) {
                // このコンポーネントが参照先集約のSingleViewの一部としてレンダリングされている場合、
                // キーがどの参照先データかは自明のため、非表示にする。
                return $$"""
                    <input type="hidden" {...register({{GetRegisterName(refProperty)}})} />
                    """;

            } else if (_mode == SingleView.E_Type.View) {
                // リンク
                var singleView = new SingleView(refProperty.MemberAggregate.GetRoot(), SingleView.E_Type.View);
                var linkKeys = refProperty.MemberAggregate
                    .GetRoot()
                    .GetKeys()
                    .OfType<AggregateMember.ValueMember>()
                    .Select(m => m.Declared.GetFullPathAsSingleViewDataClass().Join("."));

                var names = refProperty.MemberAggregate
                    .GetNames()
                    .OfType<AggregateMember.ValueMember>()
                    .Select(m => m.Declared.GetFullPathAsSingleViewDataClass().Join("."));

                return $$"""
                    <VForm.Item label="{{refProperty.MemberName}}">
                      <Link className="text-link" to={`{{singleView.GetUrlStringForReact(linkKeys.Select(k => $"getValues('{k}')"))}}`}>
                    {{names.SelectTextTemplate(k => $$"""
                        {getValues('{{k}}')}
                    """)}}
                      </Link>
                    </VForm.Item>
                    """;

            } else {
                // コンボボックス
                var registerName = GetRegisterName(refProperty);
                var combobox = new ComboBox(refProperty.MemberAggregate);
                var component = _mode switch {
                    SingleView.E_Type.Create => combobox.RenderCaller(registerName, "className='w-full'"),
                    SingleView.E_Type.Edit => combobox.RenderCaller(registerName, "className='w-full'", IfReadOnly("readOnly", refProperty)),
                    _ => throw new NotImplementedException(),
                };
                return $$"""
                    <VForm.Item label="{{refProperty.MemberName}}">
                      {{WithIndent(component, "  ")}}
                    </VForm.Item>
                    """;
            }
        }

        private string RenderProperty(AggregateMember.Schalar schalar) {
            if (schalar.Options.InvisibleInGui) {
                return $$"""
                    <input type="hidden" {...register({{GetRegisterName(schalar)}})} />
                    """;

            } else {
                var reactComponent = schalar.Options.MemberType.GetReactComponent(new GetReactComponentArgs {
                    Type = GetReactComponentArgs.E_Type.InDetailView,
                });

                // read only
                if (_mode == SingleView.E_Type.View) {
                    reactComponent.Props.Add("readOnly", string.Empty);

                } else if (_mode == SingleView.E_Type.Edit
                           && schalar is AggregateMember.ValueMember vm && vm.IsKey) {
                    reactComponent.Props.Add("readOnly", $"item?.{SingleViewDataClass.IS_LOADED}");
                }

                return $$"""
                    <VForm.Item label="{{schalar.MemberName}}">
                      <{{reactComponent.Name}} {...registerEx({{GetRegisterName(schalar)}})}{{string.Concat(reactComponent.GetPropsStatement())}} />
                    </VForm.Item>
                    """;
            }
        }

        #region 部品
        private string GetComponentName() {
            var entry = _aggregate.GetEntry().As<Aggregate>();
            if (_aggregate.IsInTreeOf(entry)) {
                return $"{_aggregate.Item.TypeScriptTypeName}View";

            } else {
                var path = _aggregate
                    .PathFromEntry()
                    .Select(edge => edge.RelationName.ToCSharpSafe())
                    .Join("_");
                return $"{path}_{_aggregate.Item.TypeScriptTypeName}View";
            }
        }

        private IReadOnlyList<string> GetArguments() {
            // 祖先コンポーネントの中に含まれるChildrenの数だけ、
            // このコンポーネントのその配列中でのインデックスが特定されている必要があるので、引数で渡す
            return _aggregate
                .PathFromEntry()
                .Where(edge => edge.Terminal != _aggregate
                            && edge.Terminal.As<Aggregate>().IsChildrenMember())
                .Select((_, i) => $"index_{i}")
                .ToArray();
        }

        private IEnumerable<AggregateMember.AggregateMemberBase> GetMembers() {
            return new TransactionScopeDataClass(_aggregate).GetOwnMembers();
        }

        private string GetRegisterName(AggregateMember.AggregateMemberBase? prop = null) {
            return GetRegisterName(_aggregate, prop);
        }

        /// <summary>
        /// TODO: <see cref="SingleViewDataClass"/> の役割なのでそちらに移す
        /// </summary>
        internal static IEnumerable<string> GetRegisterNameBeforeJoin(
            GraphNode<Aggregate> aggregate,
            AggregateMember.AggregateMemberBase? prop = null) {

            var i = 0;
            foreach (var e in aggregate.PathFromEntry()) {
                var edge = e.As<Aggregate>();

                if (edge.IsRef()) {
                    var dataClass = new SingleViewDataClass(edge.Terminal);
                    yield return dataClass
                        .GetRefFromProps()
                        .Single(p => p.Aggregate == edge.Initial
                                  && p.Aggregate.Source == edge)
                        .PropName;
                    //if (edge.Source.As<Aggregate>() == edge.Initial) {
                    //    // aggregateが参照する側ではなく参照される側の場合
                    //    var dataClass = new SingleViewDataClass(edge.Terminal);
                    //    yield return dataClass
                    //        .GetRefFromProps()
                    //        .Single(p => p.Aggregate == edge.Initial)
                    //        .PropName;
                    //} else {
                    //    // aggregateが参照される側ではなく参照する側の場合
                    //    yield return edge.RelationName;
                    //}
                } else {
                    var dataClass = new SingleViewDataClass(edge.Initial);
                    yield return dataClass
                        .GetChildProps()
                        .Single(p => p.Aggregate == edge.Terminal)
                        .PropName;
                }

                if (edge.Terminal.IsChildrenMember()) {
                    if (edge.Terminal != aggregate) {
                        // 祖先の中にChildrenがあるので配列番号を加える
                        yield return "${index_" + i.ToString() + "}";
                        i++;
                    } else if (edge.Terminal == aggregate && prop != null) {
                        // このコンポーネント自身がChildrenのとき
                        // - propがnull: useArrayFieldの登録名の作成なので配列番号を加えない
                        // - propがnullでない: mapの中のプロパティのレンダリングなので配列番号を加える
                        yield return "${index_" + i.ToString() + "}";
                        i++;
                    }
                }
            }
            if (prop != null) {
                yield return SingleViewDataClass.OWN_MEMBERS;
                yield return prop.MemberName;
            }
        }
        /// <summary>
        /// TODO: リファクタリング
        /// </summary>
        private static string GetRegisterName(GraphNode<Aggregate> aggregate, AggregateMember.AggregateMemberBase? prop = null) {
            var name = GetRegisterNameBeforeJoin(aggregate, prop).Join(".");
            return string.IsNullOrEmpty(name) ? string.Empty : $"`{name}`";
        }

        private string IfReadOnly(string readOnly, AggregateMember.AggregateMemberBase prop) {
            return _mode switch {
                SingleView.E_Type.Create => "",
                SingleView.E_Type.View => readOnly,
                SingleView.E_Type.Edit
                    => prop is AggregateMember.ValueMember vm && vm.IsKey
                    || prop is AggregateMember.Ref @ref && @ref.Relation.IsPrimary()
                        ? $"{readOnly}={{item?.{SingleViewDataClass.IS_LOADED}}}"
                        : $"",
                _ => throw new NotImplementedException(),
            };
        }
        #endregion 部品
    }
}
