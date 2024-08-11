using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Boolean : IAggregateMemberType {
        public string GetCSharpTypeName() => "bool";
        public string GetTypeScriptTypeName() => "boolean";
        public SearchBehavior SearchBehavior => SearchBehavior.Strict;

        public ReactInputComponent GetReactComponent() {
            return new ReactInputComponent {
                Name = "Input.CheckBox",
            };
        }

        public IGridColumnSetting GetGridColumnEditSetting() {
            return new ComboboxColumnSetting {
                OptionItemTypeName = $"{{ key: 'T' | 'F', text: string }}",
                Options = $"[{{ key: 'T' as const, text: '✓' }}, {{ key: 'F' as const, text: '' }}]",
                EmitValueSelector = $"opt => opt",
                MatchingKeySelectorFromEmitValue = $"opt => opt.key",
                MatchingKeySelectorFromOption = $"opt => opt.key",
                TextSelector = $"opt => opt.text",

                GetValueFromRow = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? { key: 'T' as const, text: '✓' } : { key: 'F' as const, text: '' }
                    """,

                GetDisplayText = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? '✓' : ''
                    """,
                SetValueToRow = (value, formatted) => $$"""
                    const {{formatted}} = {{value}}?.key === 'T'
                    """,

                OnClipboardCopy = (value, formatted) => $$"""
                    const {{formatted}} = {{value}} ? '✓' : ''
                    """,
                OnClipboardPaste = (value, formatted) => $$"""
                    const normalized = {{value}}.trim().toUpperCase()
                    const {{formatted}} = normalized !== ''
                      && normalized !== 'FALSE'
                      && normalized !== '0'
                    """,
            };
        }


        private const string BOOL_SEARCH_CONDITION_ENUM = "E_BoolSearchCondition";
        private const string NO_FILTER = "指定なし";
        private const string ONLY_TRUE = "Trueのみ";
        private const string ONLY_FALSE = "Falseのみ";

        public string GetSearchConditionCSharpType() => BOOL_SEARCH_CONDITION_ENUM;
        public string GetSearchConditionTypeScriptType() => $"'{NO_FILTER}' | '{ONLY_TRUE}' | '{ONLY_FALSE}'";

        void IAggregateMemberType.GenerateCode(CodeRenderingContext context) {
            context.CoreLibrary.Enums.Add($$"""
                public enum {{BOOL_SEARCH_CONDITION_ENUM}} {
                    {{NO_FILTER}},
                    {{ONLY_TRUE}},
                    {{ONLY_FALSE}},
                }
                """);
        }

        string IAggregateMemberType.RenderFilteringStatement(AggregateMember.ValueMember member, string query, string searchCondition, E_SearchConditionObject searchConditionObject, E_SearchQueryObject searchQueryObject) {
            var isArray = member.Owner.EnumerateAncestorsAndThis().Any(a => a.IsChildrenMember());
            var pathFromSearchCondition = searchConditionObject == E_SearchConditionObject.SearchCondition
                ? member.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.CSharp)
                : member.Declared.GetFullPathAsRefSearchConditionFilter(E_CsTs.CSharp);
            var fullpathNullable = $"{searchCondition}.{pathFromSearchCondition.Join("?.")}";
            var fullpathNotNull = $"{searchCondition}.{pathFromSearchCondition.Join(".")}";
            var entityOwnerPath = member.Owner.GetFullPathAsDbEntity().Join(".");
            var entityMemberPath = member.GetFullPathAsDbEntity().Join(".");

            return $$"""
                if ({{fullpathNullable}} == {{BOOL_SEARCH_CONDITION_ENUM}}.{{ONLY_TRUE}}) {
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{entityOwnerPath}}.Any(y => y.{{member.MemberName}} == true));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{entityMemberPath}} == true);
                """)}}
                } else if ({{fullpathNullable}} == {{BOOL_SEARCH_CONDITION_ENUM}}.{{ONLY_FALSE}}) {
                {{If(isArray, () => $$"""
                    {{query}} = {{query}}.Where(x => x.{{entityOwnerPath}}.Any(y => y.{{member.MemberName}} == false));
                """).Else(() => $$"""
                    {{query}} = {{query}}.Where(x => x.{{entityMemberPath}} == false);
                """)}}
                }
                """;
        }

        string IAggregateMemberType.RenderSearchConditionVFormBody(AggregateMember.ValueMember vm, ReactPageRenderingContext ctx) {
            var component = GetReactComponent();
            var fullpath = ctx.RenderingObjectType switch {
                E_ReactPageRenderingObjectType.SearchCondition => vm.Declared.GetFullPathAsSearchConditionFilter(E_CsTs.TypeScript).Join("."),
                E_ReactPageRenderingObjectType.RefTarget => vm.Declared.GetFullPathAsDataClassForRefTarget().Join("."),
                E_ReactPageRenderingObjectType.DataClassForDisplay => vm.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.TypeScript).Join("."),
                _ => throw new NotImplementedException(),
            };

            return $$"""
                <{{component.Name}} {...{{ctx.Register}}(`{{fullpath}}`)}{{component.GetPropsStatement().Join("")}} />
                """;
        }
    }
}
