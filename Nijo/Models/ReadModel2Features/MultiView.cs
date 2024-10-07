using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebClient.DataTable;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 一覧画面
    /// </summary>
    internal class MultiView : IReactPage {
        internal MultiView(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        public string Url => $"/{(_aggregate.Item.Options.LatinName ?? _aggregate.Item.UniqueId).ToKebabCase()}"; // React Router は全角文字非対応なので
        public string DirNameInPageDir => _aggregate.Item.PhysicalName.ToFileNameSafe();
        public string ComponentPhysicalName => $"{_aggregate.Item.PhysicalName}MultiView";
        public bool ShowMenu => true;
        public string? LabelInMenu => _aggregate.Item.DisplayName;

        internal const string PAGE_SIZE_COMBO_SETTING = "pageSizeComboSetting";
        internal const string SORT_COMBO_SETTING = "sortComboSetting";
        internal const string SORT_COMBO_FILTERING = "onFilterSortCombo";

        public SourceFile GetSourceFile() => new SourceFile {
            FileName = "multi-view.tsx",
            RenderContent = context => {
                var searchCondition = new SearchCondition(_aggregate);
                var searchResult = new DataClassForDisplay(_aggregate);
                var loadMethod = new LoadMethod(_aggregate);
                var singleView = new SingleView(_aggregate);
                var multiEditView = new MultiViewEditable(_aggregate);

                const string TO_DETAIL_VIEW = "navigateToSingleView";
                const string TO_MULTI_EDIT_VIEW = "navigateToMultiEditView";

                var pageRenderingContext = new FormUIRenderingContext {
                    CodeRenderingContext = context,
                    Register = "registerExCondition",
                    GetReactHookFormFieldPath = vm => vm.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript),
                    RenderReadOnlyStatement = vm => string.Empty, // 検索条件欄の項目が読み取り専用になることはない
                    RenderErrorMessage = vm => throw new InvalidOperationException("検索条件欄では項目ごとにエラーメッセージを表示するという概念が無い"),
                };

                var tableBuilder = new DataTableBuilder(_aggregate, $"AggregateType.{searchResult.TsTypeName}", false, _ => "() => {}")
                    // 行ヘッダ（詳細リンク）
                    .Add(new AdhocColumn {
                        Header = string.Empty,
                        DefaultWidth = 64,
                        EnableResizing = false,
                        CellContents = (ctx, arg, argRowObject) => $$"""
                        {{arg}} => (
                          <button type="button" onClick={() => {{TO_DETAIL_VIEW}}({{argRowObject}}, 'readonly')} className="text-color-link whitespace-nowrap px-1">詳細</button>
                        )
                        """,
                    })
                    // メンバーの列
                    .AddMembers(searchResult);

                return $$"""
                    import React, { useCallback, useEffect, useMemo, useRef, useState, useReducer } from 'react'
                    import { useEvent } from 'react-use-event-hook'
                    import { Link, useLocation } from 'react-router-dom'
                    import { useFieldArray, FormProvider } from 'react-hook-form'
                    import * as Icon from '@heroicons/react/24/outline'
                    import { ImperativePanelHandle, Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
                    import * as Util from '../../util'
                    import * as Input from '../../input'
                    import * as Layout from '../../collection'
                    import * as AggregateType from '../../autogenerated-types'
                    import * as AggregateHook from '../../autogenerated-hooks'
                    import { {{AutoGeneratedCustomizer.USE_CONTEXT}} } from '../../autogenerated-customizer'

                    const VForm2 = Layout.VForm2

                    export default function () {
                      const [, dispatchMsg] = Util.useMsgContext()
                      const [, dispatchToast] = Util.useToastContext()
                      const { get, post } = Util.useHttpRequest()

                      // 検索条件
                      const rhfSearchMethods = Util.useFormEx<AggregateType.{{searchCondition.TsTypeName}}>({ defaultValues: AggregateType.{{searchCondition.CreateNewObjectFnName}}() })
                      const {
                        getValues: getConditionValues,
                        registerEx: registerExCondition,
                        reset: resetSearchCondition,
                        formState: { defaultValues }, // 最後に検索した時の検索条件
                      } = rhfSearchMethods

                      // 検索条件の並び順コンボボックス
                      const {{SORT_COMBO_FILTERING}} = useEvent((keyword: string | undefined) => {
                        // 既に選択されている選択肢を除外する。
                        // 同じ項目の「昇順」「降順」はどちらか片方のみ選択可能なので、末尾の昇順降順を除いた値で判定する
                        const selected = new Set(getConditionValues('{{SearchCondition.SORT_TS}}')?.map(x => x.replace(/({{SearchCondition.ASC_SUFFIX}}|{{SearchCondition.DESC_SUFFIX}})$/, '')))
                        const notSelected = SORT_COMBO_SOURCE.filter(x => {
                          const cleanedOption = x.replace(/({{SearchCondition.ASC_SUFFIX}}|{{SearchCondition.DESC_SUFFIX}})$/, '')
                          return !selected.has(cleanedOption)
                        })
                        const filtered = keyword ? notSelected.filter(x => x.includes(keyword)) : notSelected
                        return Promise.resolve(filtered)
                      })

                      // 検索結果
                      const { {{LoadMethod.LOAD}}, {{LoadMethod.COUNT}}, {{LoadMethod.CURRENT_PAGE_ITEMS}} } = AggregateHook.{{loadMethod.ReactHookName}}(true)

                      // 検索条件欄の開閉
                      const searchConditionPanelRef = useRef<ImperativePanelHandle>(null)
                      const [collapsed, setCollapsed] = useState(false)
                      const toggleSearchCondition = useCallback(() => {
                        if (searchConditionPanelRef.current?.getCollapsed()) {
                          searchConditionPanelRef.current.expand()
                        } else {
                          searchConditionPanelRef.current?.collapse()
                        }
                      }, [searchConditionPanelRef])

                      // ページング
                      const [totalItemCount, setTotalItemCount] = useState(0)
                      const paging = Input.usePager(
                        defaultValues?.skip,
                        defaultValues?.take,
                        totalItemCount,
                        skip => navigateToThis({ ...getConditionValues(), skip }))

                      // 初期表示時処理
                      const { search: locationSearch } = useLocation()
                      const executeLoading = useEvent(async () => {
                        const condition = AggregateType.{{searchCondition.ParseQueryParameter}}(locationSearch)
                        resetSearchCondition(condition) // 画面上の検索条件欄の表示を更新する

                        // URLで検索条件が指定されている場合、わざわざ画面上の検索条件欄に入力することが少ないため、検索条件欄を閉じる
                        if (locationSearch) searchConditionPanelRef.current?.collapse()

                        // 再検索
                        {{LoadMethod.COUNT}}(condition.{{SearchCondition.FILTER_TS}}).then(setTotalItemCount)
                        await {{LoadMethod.LOAD}}(condition)
                      })
                      useEffect(() => {
                        executeLoading()
                      }, [{{LoadMethod.LOAD}}, locationSearch])

                      // 再読み込み時処理
                      const navigateToThis = AggregateHook.{{NavigationHookName}}()
                      const handleReload = useCallback(() => {
                        navigateToThis(getConditionValues()) // 画面まるごと再表示
                      }, [{{LoadMethod.LOAD}}, getConditionValues, resetSearchCondition, searchConditionPanelRef])

                      // クリア時処理
                      const clearSearchCondition = useEvent(() => {
                        resetSearchCondition(AggregateType.{{searchCondition.CreateNewObjectFnName}}())
                        searchConditionPanelRef.current?.expand()
                      })

                      // 画面遷移（詳細画面）
                      const {{TO_DETAIL_VIEW}} = Util.{{singleView.GetNavigateFnName(SingleView.E_Type.ReadOnly)}}()
                      const navigateToCreateView = Util.{{singleView.GetNavigateFnName(SingleView.E_Type.New)}}()
                      const onClickCreateViewLink = useEvent(() => navigateToCreateView())

                      // 画面遷移（一括編集画面）
                      const {{TO_MULTI_EDIT_VIEW}} = AggregateHook.{{multiEditView.NavigationHookName}}()
                      const onClickMultiEditViewLink = useEvent(() => {
                        // undefinedを許容するかどうかで型エラーが出るがこの時点での検索条件は必ず何かしら指定されているはずなのでキャストする
                        {{TO_MULTI_EDIT_VIEW}}(defaultValues as AggregateType.{{searchCondition.TsTypeName}})
                      })

                      // カスタマイズ
                      const {
                        {{ComponentPhysicalName}}: Customizers,
                        {{AutoGeneratedCustomizer.CUSTOM_UI_COMPONENT}},
                      } = {{AutoGeneratedCustomizer.USE_CONTEXT}}()
                      const tableRef = useRef<Layout.DataTableRef<AggregateType.{{searchResult.TsTypeName}}>>(null)
                      const getSelectedItems = useEvent(() => {
                        return tableRef.current?.getSelectedRows().map(x => x.row) ?? []
                      })
                      const columnCustomizer = Customizers?.{{SEARCH_RESULT_CUSTOMIZER}}?.()

                      // 列定義
                      const cellType = Layout.{{CellType.USE_HELPER}}<AggregateType.{{searchResult.TsTypeName}}>()
                      const columnDefs: Layout.DataTableColumn<AggregateType.{{searchResult.TsTypeName}}>[] = useMemo(() => {
                        const defs: Layout.DataTableColumn<AggregateType.{{searchResult.TsTypeName}}>[] = [
                          {{WithIndent(tableBuilder.RenderColumnDef(context), "      ")}}
                        ]
                        return columnCustomizer?.(defs) ?? defs
                      }, [get, post, {{TO_DETAIL_VIEW}}, columnCustomizer, cellType])

                      return (
                        <Layout.PageFrame
                          header={<>
                            <Layout.PageTitle className="self-center">
                              {{_aggregate.Item.DisplayName}}
                            </Layout.PageTitle>
                            <div className="flex-1"></div>
                    {{If(!_aggregate.Item.Options.IsReadOnlyAggregate, () => $$"""
                            <Input.IconButton className="self-center" onClick={onClickMultiEditViewLink}>一括編集</Input.IconButton>
                            <Input.IconButton className="self-center" onClick={onClickCreateViewLink}>新規作成</Input.IconButton>
                    """)}}
                            {Customizers?.{{HEADER_CUSTOMIZER}} && (
                              <Customizers.{{HEADER_CUSTOMIZER}} getSelectedItems={getSelectedItems} />
                            )}
                            <Input.IconButton className="self-center" onClick={clearSearchCondition}>クリア</Input.IconButton>
                            <div className="self-center flex">
                              <Input.IconButton icon={Icon.MagnifyingGlassIcon} fill onClick={handleReload}>検索</Input.IconButton>
                              <div className="self-stretch w-px bg-color-base"></div>
                              <Input.IconButton icon={collapsed ? Icon.ChevronDownIcon : Icon.ChevronUpIcon} fill onClick={toggleSearchCondition} hideText>検索条件</Input.IconButton>
                            </div>
                          </>}
                        >

                          <PanelGroup direction="vertical">

                            {/* 検索条件欄 */}
                            <Panel ref={searchConditionPanelRef} defaultSize={30} collapsible onCollapse={setCollapsed}>
                              <div className="h-full overflow-y-scroll border border-color-4 bg-color-gutter">
                                <FormProvider {...rhfSearchMethods}>
                                  {Customizers?.{{SEARCH_CONDITION_CUSTOMIZER}} ? (
                                    <Customizers.{{SEARCH_CONDITION_CUSTOMIZER}} />
                                  ) : (
                                    {{WithIndent(searchCondition.RenderVForm2(pageRenderingContext, true), "                ")}}
                                  )}
                                </FormProvider>
                              </div>
                            </Panel>

                            <PanelResizeHandle className="h-2" />

                            {/* 検索結果欄 */}
                            <Panel className="flex flex-col gap-1">
                              <Layout.DataTable
                                ref={tableRef}
                                data={{{LoadMethod.CURRENT_PAGE_ITEMS}}}
                                columns={columnDefs}
                                className="flex-1 border border-color-4"
                              />
                              <Input.ServerSidePager {...paging} className="self-center" />
                            </Panel>
                          </PanelGroup>

                        </Layout.PageFrame>
                      )
                    }

                    /** ページ件数のコンボボックスの設定 */
                    const {{PAGE_SIZE_COMBO_SETTING}} = {
                      onFilter: (keyword: string | undefined) => Promise.resolve([20, 50, 100]),
                      getOptionText: (opt: number) => `${opt}件`,
                      getValueText: (value: number) => `${value}件`,
                      getValueFromOption: (opt: number) => opt,
                    }

                    /** 初期並び順のコンボボックスの設定 */
                    const {{SORT_COMBO_SETTING}} = {
                      getOptionText: (opt: typeof SORT_COMBO_SOURCE[0]) => opt,
                      getValueText: (value: typeof SORT_COMBO_SOURCE[0]) => value,
                      getValueFromOption: (opt: typeof SORT_COMBO_SOURCE[0]) => opt,
                    }
                    /** 初期並び順のコンボボックスのデータソース */
                    const SORT_COMBO_SOURCE = [
                    {{searchCondition.GetSortLiterals().SelectTextTemplate(sort => $$"""
                      '{{sort}}' as const,
                    """)}}
                    ]
                    """;
            },
        };

        internal string NavigationHookName => $"useNavigateTo{_aggregate.Item.PhysicalName}MultiView";

        internal string RenderNavigationHook(CodeRenderingContext context) {
            var searchCondition = new SearchCondition(_aggregate);
            return $$"""
                /** {{_aggregate.Item.DisplayName}}の一覧検索画面へ遷移します。初期表示時検索条件を指定することができます。 */
                export const {{NavigationHookName}} = () => {
                  const navigate = ReactRouter.useNavigate()

                  /** {{_aggregate.Item.DisplayName}}の一覧検索画面へ遷移します。初期表示時検索条件を指定することができます。 */
                  return React.useCallback((init?: Types.{{searchCondition.TsTypeName}}) => {
                    // 初期表示時検索条件の設定
                    const searchParams = new URLSearchParams()
                    if (init !== undefined) {
                      searchParams.append('{{SearchCondition.URL_FILTER}}', JSON.stringify(init.{{SearchCondition.FILTER_TS}}))
                      if (init.{{SearchCondition.KEYWORD_TS}}) searchParams.append('{{SearchCondition.URL_KEYWORD}}', init.{{SearchCondition.KEYWORD_TS}})
                      if (init.{{SearchCondition.SORT_TS}} && init.{{SearchCondition.SORT_TS}}.length > 0) searchParams.append('{{SearchCondition.URL_SORT}}', JSON.stringify(init.{{SearchCondition.SORT_TS}}))
                      if (init.{{SearchCondition.TAKE_TS}} !== undefined) searchParams.append('{{SearchCondition.URL_TAKE}}', init.{{SearchCondition.TAKE_TS}}.toString())
                      if (init.{{SearchCondition.SKIP_TS}} !== undefined) searchParams.append('{{SearchCondition.URL_SKIP}}', init.{{SearchCondition.SKIP_TS}}.toString())
                    }

                    navigate({
                      pathname: '{{Url}}',
                      search: searchParams.toString()
                    })
                  }, [navigate])
                }
                """;
        }

        #region カスタマイズ部分
        private const string HEADER_CUSTOMIZER = "HeaderComponent";
        private const string SEARCH_CONDITION_CUSTOMIZER = "SearchConditionComponent";
        private const string SEARCH_RESULT_CUSTOMIZER = "useGridColumnCustomizer";

        internal string RenderCustomizersDeclaring() {
            var searchResult = new DataClassForDisplay(_aggregate);
            return $$"""
                {{ComponentPhysicalName}}?: {
                  {{HEADER_CUSTOMIZER}}?: (props: {
                    getSelectedItems: (() => AggregateType.{{searchResult.TsTypeName}}[])
                  }) => React.ReactNode
                  {{SEARCH_CONDITION_CUSTOMIZER}}?: () => React.ReactNode
                  {{SEARCH_RESULT_CUSTOMIZER}}?: () => ((defaultColumns: Layout.DataTableColumn<AggregateType.{{searchResult.TsTypeName}}>[]) => Layout.DataTableColumn<AggregateType.{{searchResult.TsTypeName}}>[])
                }
                """;
        }
        #endregion カスタマイズ部分
    }
}
