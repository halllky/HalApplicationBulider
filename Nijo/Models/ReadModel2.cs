using Nijo.Core;
using Nijo.Models.ReadModel2Features;
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

            context.CoreLibrary.UseAggregateFile(rootAggregate, builder => {
                foreach (var agg in rootAggregate.EnumerateThisAndDescendants()) {
                    // データ型: 検索条件クラス
                    var condition = new SearchCondition(agg);
                    builder.DataClassDeclaring.Add(condition.RenderCSharpDeclaring(context));
                    builder.TypeScriptDataTypes.Add(condition.RenderTypeScriptDeclaring(context));

                    // データ型: ビュークラス
                    var viewData = new DataClassForView(agg);
                    builder.DataClassDeclaring.Add(viewData.RenderCSharpDeclaring(context));
                    builder.TypeScriptDataTypes.Add(viewData.RenderTypeScriptDeclaring(context));
                }

                // 処理: 検索処理
                var load = new LoadMethod(rootAggregate);
                context.ReactProject.AutoGeneratedHook.Add(load.ReactHookName, load.RenderReactHook(context));
                builder.ControllerActions.Add(load.RenderControllerAction(context));
                builder.AppServiceMethods.Add(load.RenderAppSrvAbstractMethod(context));
                builder.AppServiceMethods.Add(load.RenderAppSrvBaseMethod(context));
            });

            // UI: MultiView
            var multiView = new MultiView(rootAggregate);
            context.ReactProject.AddPage(multiView.Render());

            // UI: SingleView
            var createView = new SingleView(rootAggregate, SingleView.E_Type.New);
            var readOnlyView = new SingleView(rootAggregate, SingleView.E_Type.ReadOnly);
            var editView = new SingleView(rootAggregate, SingleView.E_Type.Edit);
            context.ReactProject.AddPage(createView.Render());
            context.ReactProject.AddPage(readOnlyView.Render());
            context.ReactProject.AddPage(editView.Render());
        }

        void IModel.GenerateCode(CodeRenderingContext context) {

            // 処理: 一括更新処理
            var batchUpdate = new BatchUpdateViewData();
            context.ReactProject.AutoGeneratedHook.Add(batchUpdate.ReactHookName, batchUpdate.RenderReactHook(context));
            context.WebApiProject.ControllerDir(controllerDir => {
                controllerDir.Generate(batchUpdate.RenderController());
            });
            context.CoreLibrary.AppSrvMethods.Add(batchUpdate.RenderAppSrvMethod(context));
        }
    }
}
