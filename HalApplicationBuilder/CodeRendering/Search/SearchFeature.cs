using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Search {
    /// <summary>
    /// 検索機能
    /// </summary>
    internal partial class SearchFeature {
        internal SearchFeature(GraphNode<EFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            DbEntity = dbEntity.AsEntry();
            Context = ctx;

            var aggregate = DbEntity.GetCorrespondingAggregate();
            DisplayName = aggregate == null ? string.Empty : aggregate.Item.DisplayName;
            ReactPageUrl = aggregate == null ? string.Empty : $"/{aggregate.Item.UniqueId}";
        }

        internal CodeRenderingContext Context { get; }
        internal GraphNode<EFCoreEntity> DbEntity { get; }

        internal string DisplayName { get; }
        internal string PhysicalName => DbEntity.Item.ClassName;

        internal string SearchConditionClassName => $"{PhysicalName}SearchCondition";
        internal string SearchResultClassName => $"{PhysicalName}SearchResult";
        internal string DbContextSearchMethodName => $"Search{PhysicalName}";

        internal string ReactPageUrl { get; }
        internal const string REACT_FILENAME = "list.tsx";

        private const string SEARCHCONDITION_BASE_CLASS_NAME = "SearchConditionBase";
        private const string SEARCHCONDITION_PAGE_PROP_NAME = "__halapp__Page";

        private const string SEARCHRESULT_BASE_CLASS_NAME = "SearchResultBase";
        private const string SEARCHRESULT_INSTANCE_KEY_PROP_NAME = "__halapp__InstanceKey";
        private const string SEARCHRESULT_INSTANCE_NAME_PROP_NAME = "__halapp__InstanceName";

        private ICollection<Member>? _members;
        private ICollection<Member> Members {
            get {
                _members ??= DbEntity
                    .EnumerateThisAndDescendants()
                    .Where(entity => entity == DbEntity
                                    || entity.IsChildMember()
                                    || entity.Source?.IsRef() == true)
                    .SelectMany(entity => entity.GetColumns())
                    .Select(col => new Member {
                        CorrespondingDbColumn = col,
                        IsInstanceKey = col.IsPrimary && col.Owner == DbEntity,
                        IsInstanceName = col.IsInstanceName && col.Owner == DbEntity,
                        Type = col.MemberType,
                        ConditionPropName = col.Owner
                            .PathFromEntry()
                            .Select(edge => edge.RelationName)
                            .Concat(new[] { col.PropertyName })
                            .Join("_"),
                        SearchResultPropName = col.Owner
                            .PathFromEntry()
                            .Select(edge => edge.RelationName)
                            .Concat(new[] { col.PropertyName })
                            .Join("_"),
                    })
                    .ToArray();
                return _members;
            }
        }
        internal class Member {
            internal required string ConditionPropName { get; init; }
            internal required string SearchResultPropName { get; init; }
            internal required EFCoreEntity.Member CorrespondingDbColumn { get; init; }
            internal required IAggregateMemberType Type { get; init; }
            internal required bool IsInstanceKey { get; init; }
            internal required bool IsInstanceName { get; init; }
        }
    }
}
