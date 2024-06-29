using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts {
    internal class App {

        internal List<Func<string, string>> ConfigureServices { get; } = new List<Func<string, string>>();
        internal List<Func<string, string>> ConfigureServicesWhenBatchProcess { get; } = new List<Func<string, string>>();

        internal List<string> AppSrvMethods { get; } = new List<string>();
        internal List<string> DashBoardImports { get; } = new List<string>();
        internal List<string> DashBoardContents { get; } = new List<string>();

        internal readonly Dictionary<GraphNode<Aggregate>, AggregateFile> _itemsByAggregate = new();
        internal void Aggregate(GraphNode<Aggregate> aggregate, Action<AggregateFile> fn) {
            if (!_itemsByAggregate.TryGetValue(aggregate, out var item)) {
                item = new AggregateFile(aggregate);
                _itemsByAggregate.Add(aggregate, item);
            }
            fn(item);
        }

        internal void GenerateCode(CodeRenderingContext context) {
            context.WebApiProject.AutoGeneratedDir(genDir => {
                genDir.Generate(Configure.Render(
                    context,
                    ConfigureServicesWhenBatchProcess,
                    ConfigureServices));
                genDir.Generate(EnumDefs.Render(context));

                // アプリケーションサービス
                Customize.RenderBaseClasses(context);
                genDir.Generate(new ApplicationService().Render(context, AppSrvMethods));

                genDir.Directory("Web", controllerDir => {

                });
                genDir.Directory("EntityFramework", efDir => {
                    var onModelCreating = _itemsByAggregate
                        .Where(x => x.Value.OnModelCreating.Any())
                        .Select(x => $"OnModelCreating_{x.Key.Item.PhysicalName}");
                    efDir.Generate(new DbContextClass(context.Config).RenderDeclaring(context, onModelCreating));
                });

                foreach (var aggFile in _itemsByAggregate.Values) {
                    genDir.Generate(aggFile.Render(context));
                }

                // ユニットテスト用コード
                if (context.Options.OverwriteConcreteAppSrvFile) {
                    genDir.Directory("..", outOfGenDir => {
                        outOfGenDir.Generate(new ApplicationService().RenderConcreteClass());
                    });
                }
            });

            context.WebApiProject.UtilDir(utilDir => {
                utilDir.Generate(RuntimeSettings.Render(context));
                utilDir.Generate(Parts.Utility.DotnetExtensions.Render(context));
                utilDir.Generate(Parts.Utility.FromTo.Render(context));
                utilDir.Generate(Parts.Utility.UtilityClass.RenderJsonConversionMethods(context));
            });

            context.ReactProject.AutoGeneratedDir(reactDir => {

                reactDir.CopyEmbeddedResource(context.EmbeddedResources
                    .Get("react", "src", "__autoGenerated", "index.tsx"));
                reactDir.CopyEmbeddedResource(context.EmbeddedResources
                    .Get("react", "src", "__autoGenerated", "nijo-default-style.css"));

                reactDir.Generate(TypesTsx.Render(context, _itemsByAggregate.Select(x => KeyValuePair.Create(x.Key, x.Value.TypeScriptDataTypes))));
                reactDir.Generate(MenuTsx.Render(context));

                reactDir.Directory("collection", layoutDir => {
                    var resources = context.EmbeddedResources
                        .Enumerate("react", "src", "__autoGenerated", "collection");
                    foreach (var resource in resources) {
                        layoutDir.CopyEmbeddedResource(resource);
                    }
                });
                reactDir.Directory("input", userInputDir => {
                    var resources = context.EmbeddedResources
                        .Enumerate("react", "src", "__autoGenerated", "input");
                    foreach (var resource in resources) {
                        userInputDir.CopyEmbeddedResource(resource);
                    }

                    // TODO: どの集約がコンボボックスを作るのかをModelsが決められるようにしたい
                    userInputDir.Generate(ComboBox.RenderDeclaringFile(context));
                });
            });

            context.ReactProject.UtilDir(reactUtilDir => {
                var resources = context.EmbeddedResources
                    .Enumerate("react", "src", "__autoGenerated", "util");
                foreach (var resource in resources) {
                    reactUtilDir.CopyEmbeddedResource(resource);
                }

                // TODO: Modelsが決められるようにしたい
                reactUtilDir.Generate(NavigationWrapper.Render());
            });

            context.ReactProject.PagesDir(pageDir => {

                pageDir.Generate(DashBoard.Generate(context, this));

                var resources = context.EmbeddedResources
                    .Enumerate("react", "src", "__autoGenerated", "pages");
                foreach (var resource in resources) {
                    pageDir.CopyEmbeddedResource(resource);
                }

                foreach (var group in context.ReactProject.ReactPages.GroupBy(p => p.DirNameInPageDir)) {
                    pageDir.Directory(group.Key, aggregatePageDir => {
                        foreach (var page in group) {
                            aggregatePageDir.Generate(page.GetSourceFile());
                        }
                    });
                }
            });
        }
    }
}
