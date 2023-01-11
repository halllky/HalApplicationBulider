﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.Core.Members {
    internal class Reference : AggregateMemberBase {
        internal Reference(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => false;

        private Aggregate _refTarget;
        private Aggregate RefTarget {
            get {
                if (_refTarget == null) {
                    var type = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    _refTarget = AppSchema.FindByType(type);
                    if (_refTarget == null) throw new InvalidOperationException($"{UnderlyingPropertyInfo.Name} の型 {type.FullName} の集約が定義されていません。");
                }
                return _refTarget;
            }
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            var refTargetDbEntity = DbSchema.GetDbEntity(RefTarget);
            // 参照先DBの主キー
            foreach (var foreignKey in refTargetDbEntity.PKColumns) {
                yield return new DbColumn {
                    CSharpTypeName = foreignKey.CSharpTypeName,
                    PropertyName = $"{Name}_{foreignKey.PropertyName}",
                };
            }
            // ナビゲーションプロパティ
            yield return new DbColumn {
                Virtual = true,
                CSharpTypeName = refTargetDbEntity.RuntimeFullName,
                PropertyName = Name,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = typeof(ReferenceDTO).FullName,
                PropertyName = SearchConditonPropName,
                Initializer = "new()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = "string",
                PropertyName = SearchResultPropName,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = typeof(ReferenceDTO).FullName,
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        private string SearchConditonPropName => Name;
        private string SearchResultPropName => Name;
        private string InstanceModelPropName => Name;

        public override string RenderSearchConditionView(ViewRenderingContext context) {
            var nestedKey = context.Nest(SearchConditonPropName).Nest(nameof(ReferenceDTO.InstanceKey));
            var nestedText = context.Nest(SearchConditonPropName).Nest(nameof(ReferenceDTO.InstanceName));
            var template = new ReferenceInstanceTemplate {
                AspForKey = nestedKey.AspForPath,
                AspForText = nestedText.AspForPath,
            };
            return template.TransformText();
        }

        public override string RenderSearchResultView(ViewRenderingContext context) {
            var nestedKey = context.Nest(SearchResultPropName).Nest(nameof(ReferenceDTO.InstanceKey));
            var nestedText = context.Nest(SearchResultPropName).Nest(nameof(ReferenceDTO.InstanceName));
            return $"<span>@{nestedText.Path}<input type=\"hidden\" asp-for=\"{nestedKey.AspForPath}\"></span>";
        }

        public override string RenderInstanceView(ViewRenderingContext context) {
            var nestedKey = context.Nest(InstanceModelPropName).Nest(nameof(ReferenceDTO.InstanceKey));
            var nestedText = context.Nest(InstanceModelPropName).Nest(nameof(ReferenceDTO.InstanceName));
            var template = new ReferenceInstanceTemplate {
                AspForKey = nestedKey.AspForPath,
                AspForText = nestedText.AspForPath,
            };
            return template.TransformText();
        }

        public override void MapUIToDB(object instance, object dbEntity, RuntimeContext context) {
            // TODO
        }

        public override void MapDBToUI(object dbInstance, object uiInstance, RuntimeContext context) {
            // TODO
        }

        public override void BuildSelectStatement(SelectStatement selectStatement, object searchCondition, RuntimeContext context, string selectClausePrefix) {
            // TODO
        }

        public override void MapSearchResultToUI(System.Data.Common.DbDataReader reader, object searchResult, RuntimeContext context, string selectClausePrefix) {
            // TODO
        }

        public override void BuildAutoCompleteSelectStatement(SelectStatement selectStatement, string inputText, RuntimeContext context, string selectClausePrefix) {
            // TODO
        }

        public override IEnumerable<string> GetInvalidErrors() {
            yield break;
        }
    }

    partial class ReferenceInstanceTemplate {
        internal string AspForKey { get; set; }
        internal string AspForText { get; set; }
    }
}
