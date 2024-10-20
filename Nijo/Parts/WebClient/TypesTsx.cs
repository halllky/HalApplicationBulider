using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    internal class TypesTsx : ISummarizedFile {

        private readonly List<string> _sourceCodes = new();
        private readonly Dictionary<GraphNode<Aggregate>, List<string>> _sourceCodesRelatedAggregate = new();

        /// <summary>
        /// TypeScriptのデータ構造定義のソースコード（特定の集約に関連しないもの）を追加します。
        /// </summary>
        internal void Add(string sourceCode) {
            _sourceCodes.Add(sourceCode);
        }
        /// <summary>
        /// TypeScriptのデータ構造定義のソースコード（特定の集約に関連するもの）を追加します。
        /// </summary>
        internal void Add(GraphNode<Aggregate> aggregate, string sourceCode) {
            if (_sourceCodesRelatedAggregate.TryGetValue(aggregate, out var list)) {
                list.Add(sourceCode);
            } else {
                _sourceCodesRelatedAggregate.Add(aggregate, [sourceCode]);
            }
        }

        /// <summary>
        /// ほかの <see cref="ISummarizedFile"/> の中で列挙体が生成されることがあるので
        /// </summary>
        int ISummarizedFile.RenderingOrder => 999;

        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.ReactProject.AutoGeneratedDir(dir => {
                dir.Generate(new SourceFile {
                    FileName = "autogenerated-types.ts",
                    RenderContent = context => $$"""
                        import { UUID } from 'uuidjs'
                        import *  as Input from './input'
                        import * as Util from './util'

                        {{_sourceCodes.SelectTextTemplate(source => $$"""
                        {{source}}

                        """)}}
                        {{_sourceCodesRelatedAggregate.SelectTextTemplate(item => $$"""
                        // ------------------ {{item.Key.Item.DisplayName}} ------------------
                        {{item.Value.SelectTextTemplate(source => $$"""
                        {{source}}

                        """)}}

                        """)}}
                        """,
                });
            });
        }
    }
}
