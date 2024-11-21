using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts.WebClient;
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
            var uiContext = context.UseSummarizedFile<UiContext>();

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

            // 処理: 検索処理の最後に読み取り専用を設定
            aggregateFile.AppServiceMethods.Add(rootDisplayData.RenderSetKeysReadOnly(context));

            // データ型: 一括更新処理 エラーメッセージの入れ物
            context.UseSummarizedFile<SaveContext>().AddReadModel(rootAggregate);

            // 処理: 一括更新処理
            context.UseSummarizedFile<BatchUpdateReadModel>().Register(rootAggregate);

            // 処理: 一括更新処理前関数（ディープイコール比較関数、変更比較関数）
            context.ReactProject.Types.Add(rootDisplayData.RenderDeepEqualFunctionRecursively(context));
            context.ReactProject.Types.Add(rootDisplayData.RenderCheckChangesFunction(context));

            // UI: MultiView
            var multiView = new MultiView(rootAggregate);
            context.ReactProject.AutoGeneratedHook.Add(multiView.RenderNavigationHook(context));
            multiView.RegisterUiContext(uiContext);
            context.CoreLibrary.AppSrvMethods.Add(multiView.RenderAppSrvGetUrlMethod());
            context.ReactProject.AddReactPage(
                multiView.Url,
                multiView.DirNameInPageDir,
                multiView.ComponentPhysicalName,
                multiView.ShowMenu,
                multiView.LabelInMenu,
                multiView.GetSourceFile());

            // UI: SingleView
            var singleView = new SingleView(rootAggregate);
            context.ReactProject.AutoGeneratedHook.Add(singleView.RenderPageFrameComponent(context));
            singleView.RegisterUiContext(uiContext);
            context.ReactProject.AddReactPage(
                singleView.Url,
                singleView.DirNameInPageDir,
                singleView.ComponentPhysicalName,
                singleView.ShowMenu,
                singleView.LabelInMenu,
                singleView.GetSourceFile());

            // SingleView 初期表示時サーバー側処理
            aggregateFile.AppServiceMethods.Add(singleView.RenderSetSingleViewDisplayDataFn(context));
            aggregateFile.ControllerActions.Add(singleView.RenderSetSingleViewDisplayData(context));

            // UI: SingleViewナビゲーション用関数
            context.ReactProject.UrlUtil.Add(singleView.RenderNavigateFn(context, SingleView.E_Type.New));
            context.ReactProject.UrlUtil.Add(singleView.RenderNavigateFn(context, SingleView.E_Type.Edit)); // readonly, edit は関数共用
            context.CoreLibrary.AppSrvMethods.Add(singleView.RenderAppSrvGetUrlMethod()); // サーバー側は全モードで1つのメソッド

            // UI: MultiViewEditable
            var multiViewEditable = new MultiViewEditable(rootAggregate);
            context.ReactProject.AutoGeneratedHook.Add(multiViewEditable.RenderNavigationHook(context));
            multiViewEditable.RegisterUiContext(uiContext);
            context.ReactProject.AddReactPage(
                multiViewEditable.Url,
                multiViewEditable.DirNameInPageDir,
                multiViewEditable.ComponentPhysicalName,
                multiViewEditable.ShowMenu,
                multiViewEditable.LabelInMenu,
                multiViewEditable.GetSourceFile());

            // コマンドの処理結果でこの集約の詳細画面に遷移できるように登録する
            context.UseSummarizedFile<CommandModelFeatures.CommandResult>().Register(rootAggregate);

            // ---------------------------------------------
            // 他の集約から参照されるときのための部品

            foreach (var agg in allAggregates) {

                // パフォーマンス改善のため、ほかの集約から参照されていない集約のRefTo部品は生成しない
                if (!context.Config.GenerateUnusedRefToModules && !agg.GetReferedEdges().Any()) {
                    continue;
                }

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
                context.ReactProject.Types.Add(rootAggregate, refDisplayData.RenderTsNewObjectFunction(context));

                // UI: 詳細画面用のVFormの一部
                // UI: 検索条件欄のVFormの一部
                // UI: コンボボックス
                // UI: 検索ダイアログ
                // UI: インライン検索ビュー
                var refToFile = context.UseSummarizedFile<RefToFile>();
                var comboBox = new SearchComboBox(asEntry);
                var searchDialog = new SearchDialog(asEntry, asEntry);
                var inlineRef = new SearchInline(asEntry);
                refToFile.Add(asEntry, refDisplayData.RenderSingleViewUiComponent(context));
                refToFile.Add(asEntry, refSearchCondition.RenderUiComponent(context));
                refToFile.Add(asEntry, comboBox.Render(context));
                refToFile.Add(asEntry, searchDialog.RenderHook(context));
                refToFile.Add(asEntry, inlineRef.Render(context));
                searchDialog.RegisterUiContext(uiContext);
                refDisplayData.RegisterUiContext(uiContext);
                refSearchCondition.RegisterUiContext(uiContext);

                // UI: DataTable用の列
                var refToColumn = new DataTableRefColumnHelper(asEntry);
                context.UseSummarizedFile<Parts.WebClient.DataTable.CellType>().Add(refToColumn.Render(context));

                // 処理: 参照先検索
                var searchRef = new RefSearchMethod(asEntry, asEntry);
                refToFile.Add(asEntry, searchRef.RenderHook(context));
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
