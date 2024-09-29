using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.Utility;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    /// <summary>
    /// 画面表示されるデータ型
    /// </summary>
    internal class ReadModel2 : IModel {
        void IModel.GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var allAggregates = rootAggregate.EnumerateThisAndDescendants();
            var aggregateFile = context.CoreLibrary.UseAggregateFile(rootAggregate);
            var rootDisplayData = new DataClassForDisplay(rootAggregate);
            var customizer = context.UseSummarizedFile<Parts.WebClient.AutoGeneratedCustomizer>();

            // データ型: 検索条件クラス
            var condition = new SearchCondition(rootAggregate);
            aggregateFile.DataClassDeclaring.Add(condition.RenderCSharpDeclaringRecursively(context));
            context.ReactProject.Types.Add(rootAggregate, condition.RenderTypeScriptDeclaringRecursively(context));
            context.ReactProject.Types.Add(rootAggregate, condition.RenderCreateNewObjectFn(context));
            context.ReactProject.Types.Add(rootAggregate, condition.RenderParseQueryParameterFunction());

            foreach (var agg in allAggregates) {
                // データ型: 検索結果クラス
                var searchResult = new SearchResult(agg);
                aggregateFile.DataClassDeclaring.Add(searchResult.RenderCSharpDeclaring(context));

                // データ型: ビュークラス
                var displayData = new DataClassForDisplay(agg);
                aggregateFile.DataClassDeclaring.Add(displayData.RenderCSharpDeclaring(context));
                context.ReactProject.Types.Add(agg, displayData.RenderTypeScriptDeclaring(context));
                context.ReactProject.Types.Add(agg, displayData.RenderTsNewObjectFunction(context));
            }

            // 処理: 検索処理
            var load = new LoadMethod(rootAggregate);
            context.ReactProject.AutoGeneratedHook.Add(load.RenderReactHook(context));
            aggregateFile.ControllerActions.Add(load.RenderControllerAction(context));
            aggregateFile.AppServiceMethods.Add(load.RenderAppSrvBaseMethod(context));
            aggregateFile.AppServiceMethods.Add(load.RenderAppSrvAbstractMethod(context));

            // データ型: 一括更新処理 エラーメッセージの入れ物
            context.UseSummarizedFile<SaveContext>().AddReadModel(rootAggregate);

            // 処理: 一括更新処理
            context.UseSummarizedFile<BatchUpdateReadModel>().Register(rootAggregate);

            // 処理: 一括更新処理前関数（ディープイコール比較関数、変更比較関数）
            context.ReactProject.Types.Add(rootDisplayData.RenderDeepEqualFunctionRecursively(context));
            context.ReactProject.Types.Add(rootDisplayData.RenderCheckChangesFunction(context));

            // UI: MultiView
            var multiView = new MultiView(rootAggregate);
            context.ReactProject.Pages.Add(multiView);
            context.ReactProject.AutoGeneratedHook.Add(multiView.RenderNavigationHook(context));
            customizer.AddMember(multiView.RenderCustomizersDeclaring());

            // UI: SingleView
            var singleView = new SingleView(rootAggregate);
            context.ReactProject.Pages.Add(singleView);
            context.ReactProject.AutoGeneratedHook.Add(singleView.RenderPageFrameComponent(context));

            // UI: SingleViewナビゲーション用関数
            context.ReactProject.UrlUtil.Add(singleView.RenderNavigateFn(context, SingleView.E_Type.New));
            context.ReactProject.UrlUtil.Add(singleView.RenderNavigateFn(context, SingleView.E_Type.Edit)); // readonly, edit は関数共用
            context.CoreLibrary.AppSrvMethods.Add(singleView.RenderAppSrvGetUrlMethod()); // サーバー側は全モードで1つのメソッド

            // UI: MultiViewEditable
            var multiViewEditable = new MultiViewEditable(rootAggregate);
            context.ReactProject.Pages.Add(multiViewEditable);
            context.ReactProject.AutoGeneratedHook.Add(multiViewEditable.RenderNavigationHook(context));

            // UI: カスタムUI（生成後のソースで外から注入して、中で React context 経由で参照するコンポーネント。ValueMemberまたはRefでのみ使用）
            var allMembers = allAggregates.SelectMany(agg => agg.GetMembers());
            foreach (var m in allMembers) {
                if (m is AggregateMember.ValueMember vm) {
                    if (vm.DeclaringAggregate != vm.Owner) continue; // 親や参照先のメンバーはここに関係ない
                    if (vm.Options.SingleViewCustomUiComponentName != null) {
                        customizer.AddCustomUi(
                            vm.Options.SingleViewCustomUiComponentName,
                            $"{vm.Options.MemberType.GetTypeScriptTypeName()} | undefined",
                            vm.Options.MemberType.EnumerateSingleViewCustomFormUiAdditionalProps());
                    }
                    if (vm.Options.SearchConditionCustomUiComponentName != null) {
                        customizer.AddCustomUi(
                            vm.Options.SearchConditionCustomUiComponentName,
                            $"{vm.Options.MemberType.GetSearchConditionTypeScriptType()} | undefined",
                            vm.Options.MemberType.EnumerateSearchConditionCustomFormUiAdditionalProps());
                    }
                } else if (m is AggregateMember.Ref @ref) {
                    if (@ref.SingleViewCustomUiComponentName != null) {
                        var refTarget = new RefDisplayData(@ref.RefTo, @ref.RefTo);
                        customizer.AddCustomUi(
                            @ref.SingleViewCustomUiComponentName,
                            $"AggregateType.{refTarget.TsTypeName} | undefined",
                            []);
                    }
                    if (@ref.SearchConditionCustomUiComponentName != null) {
                        var refTarget = new RefSearchCondition.RefDescendantSearchCondition(@ref, @ref.RefTo);
                        customizer.AddCustomUi(
                            @ref.SearchConditionCustomUiComponentName,
                            $"AggregateType.{refTarget.TsFilterTypeName}", // 検索条件のfiterはundefinedになることはないので " | undeifned" 不要
                            []);
                    }
                }
            }

            // コマンドの処理結果でこの集約の詳細画面に遷移できるように登録する
            context.UseSummarizedFile<CommandModelFeatures.CommandResult>().Register(rootAggregate);

            // ---------------------------------------------
            // 他の集約から参照されるときのための部品

            foreach (var agg in allAggregates) {
                var asEntry = agg.AsEntry();

                // データ型
                var refTargetKeys = new DataClassForRefTargetKeys(asEntry, asEntry);
                var refSearchCondition = new RefSearchCondition(asEntry, asEntry);
                var refSearchResult = new RefSearchResult(asEntry, asEntry);
                var refDisplayData = new RefDisplayData(asEntry, asEntry);
                aggregateFile.DataClassDeclaring.Add(refTargetKeys.RenderCSharpDeclaringRecursively(context));
                aggregateFile.DataClassDeclaring.Add(refSearchCondition.RenderCSharpDeclaringRecursively(context));
                aggregateFile.DataClassDeclaring.Add(refSearchResult.RenderCSharp(context));
                aggregateFile.DataClassDeclaring.Add(refDisplayData.RenderCSharp(context));
                context.ReactProject.Types.Add(rootAggregate, refSearchCondition.RenderTypeScriptDeclaringRecursively(context));
                context.ReactProject.Types.Add(rootAggregate, refSearchCondition.RenderCreateNewObjectFn(context));
                context.ReactProject.Types.Add(rootAggregate, refTargetKeys.RenderTypeScriptDeclaringRecursively(context));
                context.ReactProject.Types.Add(rootAggregate, refDisplayData.RenderTypeScript(context));

                // UI: コンボボックス
                // UI: 検索ダイアログ
                // UI: インライン検索ビュー
                var comboBox = new SearchComboBox(asEntry);
                var searchDialog = new SearchDialog(asEntry, asEntry);
                var inlineRef = new SearchInline(asEntry);
                context.ReactProject.AutoGeneratedComponents.Add(comboBox.Render(context));
                context.ReactProject.AutoGeneratedComponents.Add(searchDialog.RenderHook(context));
                context.ReactProject.AutoGeneratedComponents.Add(inlineRef.Render(context));
                customizer.AddMember(searchDialog.RenderCustomizersDeclaring());

                // UI: DataTable用の列
                var refToColumn = new DataTableRefColumnHelper(asEntry);
                context.UseSummarizedFile<Parts.WebClient.DataTable.CellType>().Add(refToColumn.Render());

                // 処理: 参照先検索
                var searchRef = new RefSearchMethod(asEntry, asEntry);
                context.ReactProject.AutoGeneratedHook.Add(searchRef.RenderHook(context));
                aggregateFile.ControllerActions.Add(searchRef.RenderController(context));
                aggregateFile.AppServiceMethods.Add(searchRef.RenderAppSrvMethodOfReadModel(context));
            }
        }

        void IModel.GenerateCode(CodeRenderingContext context) {

            // ユーティリティクラス等
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(DataClassForDisplay.RenderBaseClass());
                dir.Generate(DisplayMessageContainer.RenderCSharp());
                dir.Generate(ISaveCommandConvertible.Render());
            });
            context.CoreLibrary.Enums.Add(SingleView.RenderSingleViewNavigationEnums());
        }

        IEnumerable<string> IModel.ValidateAggregate(GraphNode<Aggregate> rootAggregate) {
            foreach (var agg in rootAggregate.EnumerateThisAndDescendants()) {

                // ルート集約またはChildrenはキー必須
                if (agg.IsRoot() || agg.IsChildrenMember()) {
                    var ownKeys = agg
                        .GetKeys()
                        .Where(m => m is AggregateMember.ValueMember vm && vm.DeclaringAggregate == vm.Owner
                                 || m is AggregateMember.Ref);
                    if (!ownKeys.Any()) {
                        yield return $"{agg.Item.DisplayName}にキーが1つもありません。";
                    }
                }

                foreach (var member in agg.GetMembers()) {

                    // WriteModelからReadModelへの参照は不可
                    if (member is AggregateMember.Ref @ref
                        && @ref.RefTo.GetRoot().Item.Options.Handler != NijoCodeGenerator.Models.ReadModel2.Key
                        && @ref.RefTo.GetRoot().Item.Options.Handler != NijoCodeGenerator.Models.WriteModel2.Key) {

                        yield return $"{agg.Item.DisplayName}.{member.MemberName}: {nameof(WriteModel2)}の参照先は{nameof(ReadModel2)}または{nameof(WriteModel2)}である必要があります。";
                    }
                }
            }
        }
    }
}
