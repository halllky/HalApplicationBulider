using HalApplicationBuilder.CodeRendering.InstanceHandling;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.CodeRendering.WebClient;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.KeywordSearching {
    partial class ComboBox {
        internal ComboBox(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string ComponentName => $"ComboBox{_aggregate.Item.DisplayName.ToCSharpSafe()}";
        internal string Api => new KeywordSearchingFeature(_aggregate).GetUri();
        internal AggregateKeyName KeyName => new AggregateKeyName(_aggregate);

        internal static string RenderDeclaringFile(IEnumerable<GraphNode<Aggregate>> allAggregates) {
            return $$"""
                import React, { useCallback } from "react"
                import { useHttpRequest } from "../util"
                import { AsyncComboBox, defineCustomComponent } from "../user-input"
                import * as Types from "../types"

                {{allAggregates.Select(a => new ComboBox(a)).SelectTextTemplate((combo, index) => $$"""
                export const {{combo.ComponentName}} = defineCustomComponent<Types.{{combo.KeyName.TypeScriptTypeName}}>((props, ref) => {
                  const { get } = useHttpRequest()
                  const query = useCallback(async (keyword: string | undefined) => {
                    const response = await get<Types.{{combo.KeyName.TypeScriptTypeName}}[]>(`{{combo.Api}}`, { keyword })
                    return response.ok ? response.data : []
                  }, [get])

                  return (
                    <AsyncComboBox
                      {...props}
                      ref={ref}
                      queryKey="combo-{{combo._aggregate.Item.UniqueId}}"
                      query={query}
                      keySelector={item => JSON.stringify([{{combo.KeyName.GetKeys().Select(m => "item." + m.MemberName).Join(", ")}}])}
                      textSelector={item => `{{combo.KeyName.GetNames().Select(m => "${item." + m.MemberName + "}").Join("&nbsp;")}}`}
                    />
                  )
                })
                """)}}
                """;
        }

        internal string RenderCaller(string raectHookFormId, params string[] attrs) {
            var attributes = attrs
                .Where(str => !string.IsNullOrWhiteSpace(str))
                .Join(" ");
            return $$"""
                <Input.{{ComponentName}} raectHookFormId={{{raectHookFormId}}} {{attributes}} />
                """;
        }
    }
}
