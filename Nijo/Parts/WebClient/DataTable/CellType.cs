using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient.DataTable {
    /// <summary>
    /// DataTableの列定義生成ヘルパー関数
    /// </summary>
    public class CellType : ISummarizedFile {

        internal const string USE_HELPER = "useColumnDefHelper";
        internal const string HELPER_TYPE = "AutoGeneratedColumnDefHelper";
        internal const string RETURNS_ONE_COLUMN = "CreateCellType";
        internal const string RETURNS_MANY_COLUMN = "CreateManyCellTypes";

        internal void Add(Helper helper) {
            // まったく同じソースコードの場合は同じ型が重複してレンダリングしようとしているのでレンダリング割愛
            if (_helpers.Any(h => h.FunctionName == helper.FunctionName && h.Body == helper.Body)) return;

            _helpers.Add(helper);
        }
        private readonly List<Helper> _helpers = new();

        public class Helper {
            public required string FunctionName { get; init; }
            public required string ReturnType { get; init; }
            public required string Body { get; init; }
            /// <summary>useMemoの外側で宣言している他フック利用ステートメント</summary>
            public List<string> Uses { get; init; } = [];
            /// <summary>useMemoの依存配列の変数名</summary>
            public List<string> Deps { get; init; } = [];
        }


        public void OnEndGenerating(CodeRenderingContext context) {

            // このオプションが指定されている場合は何も生成しない
            if (context.Config.CustomizeAllUi) return;

            context.ReactProject.AutoGeneratedDir(dir => {
                dir.Directory("collection", layoutDir => {
                    layoutDir.Generate(Render());
                });
            });
        }

        private SourceFile Render() {
            return new() {
                FileName = "DataTable.CellType.tsx",
                RenderContent = ctx => {
                    return $$"""
                        import React from 'react'
                        import useEvent from 'react-use-event-hook'
                        import * as ReactHookForm from 'react-hook-form'
                        import * as Icon from '@heroicons/react/24/outline'
                        import { ColumnEditSetting, DataTableColumn } from './DataTable.Public'
                        import * as Util from '../util'
                        import * as Input from '../input'
                        import * as AggregateType from '../autogenerated-types'
                        import * as AggregateHook from '../autogenerated-hooks'
                        import * as AggregateComponent from '../autogenerated-components'
                        import * as RefTo from '../ref-to'

                        /** DataTable列定義生成ヘルパー */
                        export const {{USE_HELPER}} = <TRow extends ReactHookForm.FieldValues,>() => {
                        {{_helpers.SelectMany(h => h.Uses).SelectTextTemplate(source => $$"""
                          {{WithIndent(source, "  ")}}
                        """)}}

                          return React.useMemo((): {{HELPER_TYPE}}<TRow> => {
                        {{_helpers.Select(h => h.Body).SelectTextTemplate(source => $$"""
                            {{WithIndent(source, "    ")}}

                        """)}}
                            return {
                        {{_helpers.Select(h => h.FunctionName).SelectTextTemplate(fnName => $$"""
                              {{fnName}},
                        """)}}
                            }
                          }, [{{_helpers.SelectMany(h => h.Deps).Join(", ")}}])
                        }

                        /** {{USE_HELPER}}の型 */
                        export type {{HELPER_TYPE}}<TRow extends ReactHookForm.FieldValues> = {
                        {{_helpers.SelectTextTemplate(h => $$"""
                          {{h.FunctionName}}: {{h.ReturnType}}
                        """)}}
                        }

                        /** 列定義生成関数の型 */
                        export type {{RETURNS_ONE_COLUMN}}<TRow extends ReactHookForm.FieldValues, TValue> = (
                          header: string,
                          getValue: ((row: TRow) => TValue),
                          setValue: ((row: TRow, value: TValue, rowIndex: number) => void),
                          opt?: CellTypeHelperOptions<TRow>
                        ) => DataTableColumn<TRow>

                        /** 列定義生成関数の型 */
                        export type {{RETURNS_MANY_COLUMN}}<TRow extends ReactHookForm.FieldValues, TValue> = (
                          header: string,
                          getValue: ((row: TRow) => TValue),
                          setValue: ((row: TRow, value: TValue, rowIndex: number) => void),
                          opt?: CellTypeHelperOptions<TRow>
                        ) => DataTableColumn<TRow>[]

                        /** 列定義生成ヘルパー関数のオプション */
                        type CellTypeHelperOptions<TRow> = {
                          defaultWidthPx?: number
                          fixedWidth?: boolean
                          headerGroupName?: string
                          readOnly?: boolean | ((row: TRow, rowIndex: number) => boolean)
                        }

                        /** 表示用のレイアウトを施したセル */
                        export const PlainCell = ({ children }: {
                          children?: React.ReactNode
                        }) => {
                          return (
                            <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
                              {children}
                              &nbsp;
                            </span>
                          )
                        }
                        """;
                },
            };
        }
    }
}
