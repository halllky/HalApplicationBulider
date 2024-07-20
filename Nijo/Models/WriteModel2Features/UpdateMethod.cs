using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 既存データ更新処理
    /// </summary>
    internal class UpdateMethod {
        internal UpdateMethod(GraphNode<Aggregate> rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly GraphNode<Aggregate> _rootAggregate;

        internal string BeforeMethodName => $"Updating{_rootAggregate.Item.PhysicalName}";
        internal object MethodName => $"Update{_rootAggregate.Item.PhysicalName}";
        internal string AfterMethodName => $"Updated{_rootAggregate.Item.PhysicalName}";

        /// <summary>
        /// データ更新処理をレンダリングします。
        /// </summary>
        internal string Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var efCoreEntity = new EFCoreEntity(_rootAggregate);
            var dataClass = new DataClassForSave(_rootAggregate, DataClassForSave.E_Type.UpdateOrDelete);
            var argType = $"{DataClassForSaveBase.UPDATE_COMMAND}<{dataClass.CsClassName}>";
            var beforeSaveContext = $"{SaveContext.BEFORE_SAVE_CONTEXT}<{dataClass.ErrorDataCsClassName}>";
            var afterSaveContext = $"{SaveContext.AFTER_SAVE_CONTEXT}";

            var keys = _rootAggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>();

            return $$"""
                /// <summary>
                /// 既存の{{_rootAggregate.Item.DisplayName}}を更新します。
                /// </summary>
                public virtual void {{MethodName}}({{argType}} after, {{SaveContext.BATCH_UPDATE_CONTEXT}} saveContext) {
                    var beforeSaveContext = new {{beforeSaveContext}}(saveContext);
                    var afterDbEntity = after.{{DataClassForSaveBase.VALUES_CS}}.{{DataClassForSave.TO_DBENTITY}}();

                    // 更新に必要な項目が空の場合は処理中断
                {{keys.SelectTextTemplate(vm => $$"""
                    if (afterDbEntity.{{vm.Declared.GetFullPathAsDbEntity().Join("?.")}} == null) {
                        beforeSaveContext.Errors.Add("{{vm.MemberName}}が空です。");
                    }
                """)}}
                    if (afterDbEntity.{{EFCoreEntity.VERSION}} == null) {
                        beforeSaveContext.Errors.Add("更新時は更新前データのバージョンを指定する必要があります。");
                    }
                    if (beforeSaveContext.Errors.HasError()) {
                        return;
                    }

                    // 更新前データ取得
                    var beforeDbEntity = {{appSrv.DbContext}}.{{efCoreEntity.DbSetName}}
                        .AsNoTracking()
                        {{WithIndent(efCoreEntity.RenderInclude(), "        ")}}
                        .SingleOrDefault(e {{WithIndent(keys.SelectTextTemplate((vm, i) => $$"""
                                           {{(i == 0 ? "=>" : "&&")}} e.{{vm.GetFullPathAsDbEntity().Join(".")}} == afterDbEntity.{{vm.Declared.GetFullPathAsDbEntity().Join(".")}}
                                           """), "                           ")}});
                    if (beforeDbEntity == null) {
                        beforeSaveContext.Errors.Add("更新対象のデータが見つかりません。");
                        return;
                    }
                    if (beforeDbEntity.{{EFCoreEntity.VERSION}} != afterDbEntity.{{EFCoreEntity.VERSION}}) {
                        beforeSaveContext.Errors.Add("ほかのユーザーが更新しました。");
                        return;
                    }

                    // 自動的に設定される項目
                    afterDbEntity.{{EFCoreEntity.VERSION}}++;
                    afterDbEntity.{{EFCoreEntity.UPDATED_AT}} = {{ApplicationService.CURRENT_TIME}};
                    afterDbEntity.{{EFCoreEntity.UPDATE_USER}} = {{ApplicationService.CURRENT_USER}};

                    // 更新前処理。入力検証や自動補完項目の設定を行う。
                    {{BeforeMethodName}}(beforeDbEntity, afterDbEntity, beforeSaveContext);

                    // エラーやコンファームがある場合は処理中断
                    if (beforeSaveContext.Errors.HasError()) return;
                    if (!beforeSaveContext.IgnoreConfirm && beforeSaveContext.HasConfirm()) return;

                    // 更新実行
                    try {
                        var entry = {{appSrv.DbContext}}.Entry(afterDbEntity);
                        entry.State = EntityState.Modified;
                        entry.Property(e => e.{{EFCoreEntity.VERSION}}).OriginalValue = beforeDbEntity.{{EFCoreEntity.VERSION}};

                        {{WithIndent(RenderDescendantAttaching(), "        ")}}

                        {{appSrv.DbContext}}.SaveChanges();
                    } catch (DbUpdateException ex) {
                        beforeSaveContext.Errors.Add(ex);
                        return;
                    }

                    // 更新後処理
                    var afterSaveContext = new {{afterSaveContext}}();
                    {{AfterMethodName}}(beforeDbEntity, afterDbEntity, afterSaveContext);
                }

                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の更新前に実行されます。
                /// エラーチェック、ワーニング、自動算出項目の設定などを行います。
                /// </summary>
                protected virtual void {{BeforeMethodName}}({{efCoreEntity.ClassName}} beforeDbEntity, {{efCoreEntity.ClassName}} afterDbEntity, {{beforeSaveContext}} context) {
                    // このメソッドをオーバーライドしてエラーチェック等を記述してください。
                }
                /// <summary>
                /// {{_rootAggregate.Item.DisplayName}}の更新SQL発行後、コミット前に実行されます。
                /// </summary>
                protected virtual void {{AfterMethodName}}({{efCoreEntity.ClassName}} beforeDbEntity, {{efCoreEntity.ClassName}} afterDbEntity, {{afterSaveContext}} context) {
                    // このメソッドをオーバーライドして必要な更新後処理を記述してください。
                }
                """;
        }


        /// <summary>
        /// DbContextに更新後の子孫要素のエンティティをアタッチさせる処理をレンダリングします。
        /// </summary>
        private string RenderDescendantAttaching() {
            var dbContext = new ApplicationService().DbContext;
            var builder = new StringBuilder();

            var descendantDbEntities = _rootAggregate.EnumerateDescendants().ToArray();
            for (int i = 0; i < descendantDbEntities.Length; i++) {
                var paths = descendantDbEntities[i].PathFromEntry().ToArray();

                // before, after それぞれの子孫インスタンスを一次配列に格納する
                void RenderEntityArray(bool renderBefore) {
                    if (paths.Any(path => path.Terminal.As<Aggregate>().IsChildrenMember())) {
                        // 子集約までの経路の途中に配列が含まれる場合
                        builder.AppendLine($"var arr{i}_{(renderBefore ? "before" : "after")} = {(renderBefore ? "beforeDbEntity" : "afterDbEntity")}");

                        var select = false;
                        foreach (var path in paths) {
                            if (select && path.Terminal.As<Aggregate>().IsChildrenMember()) {
                                builder.AppendLine($"    .SelectMany(x => x.{path.RelationName})");
                            } else if (select) {
                                builder.AppendLine($"    .Select(x => x.{path.RelationName})");
                            } else {
                                builder.AppendLine($"    .{path.RelationName}?");
                                if (path.Terminal.As<Aggregate>().IsChildrenMember()) select = true;
                            }
                        }
                        builder.AppendLine($"    .OfType<{descendantDbEntities[i].Item.EFCoreEntityClassName}>()");
                        builder.AppendLine($"    ?? Enumerable.Empty<{descendantDbEntities[i].Item.EFCoreEntityClassName}>();");

                    } else {
                        // 子集約までの経路の途中に配列が含まれない場合
                        builder.AppendLine($"var arr{i}_{(renderBefore ? "before" : "after")} = new {descendantDbEntities[i].Item.EFCoreEntityClassName}?[] {{");
                        builder.AppendLine($"    {(renderBefore ? "beforeDbEntity" : "afterDbEntity")}.{paths.Select(p => p.RelationName).Join("?.")},");
                        builder.AppendLine($"}}.OfType<{descendantDbEntities[i].Item.EFCoreEntityClassName}>().ToArray();");
                    }
                }
                RenderEntityArray(true);
                RenderEntityArray(false);

                // ChangeState変更
                builder.AppendLine($"foreach (var a in arr{i}_after) {{");
                builder.AppendLine($"    var b = arr{i}_before.SingleOrDefault(b => b.{EFCoreEntity.KEYEQUALS}(a));");
                builder.AppendLine($"    if (b == null) {{");
                builder.AppendLine($"        {dbContext}.Entry(a).State = EntityState.Added;");
                builder.AppendLine($"    }} else {{");
                builder.AppendLine($"        {dbContext}.Entry(a).State = EntityState.Modified;");
                builder.AppendLine($"    }}");
                builder.AppendLine($"}}");

                builder.AppendLine($"foreach (var b in arr{i}_before) {{");
                builder.AppendLine($"    var a = arr{i}_after.SingleOrDefault(a => a.{EFCoreEntity.KEYEQUALS}(b));");
                builder.AppendLine($"    if (a == null) {{");
                builder.AppendLine($"        {dbContext}.Entry(b).State = EntityState.Deleted;");
                builder.AppendLine($"    }}");
                builder.AppendLine($"}}");
            }

            return builder.ToString();
        }

    }
}
