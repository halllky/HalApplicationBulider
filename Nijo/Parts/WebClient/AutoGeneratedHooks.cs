using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    /// <summary>
    /// 自動生成されるReact hook。
    /// 特に必要性はないが全部1か所にまとめている。
    /// </summary>
    internal class AutoGeneratedHooks {
        internal SourceFile Render() => new SourceFile {
            FileName = "useAutoGeneratedHooks.ts",
            RenderContent = ctx => {
                return $$"""
                    export const useAutoGeneratedHooks = () => {

                    {{ctx.ReactProject.AutoGeneratedHook.SelectTextTemplate(kv => $$"""
                      {{WithIndent(kv.Value, "  ")}}

                    """)}}
                      return {
                    {{ctx.ReactProject.AutoGeneratedHook.SelectTextTemplate(kv => $$"""
                        {{kv.Key}},
                    """)}}
                      }
                    }
                    """;
            },
        };
    }
}
