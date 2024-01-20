using Nijo.Core;
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

        internal const string ASP_UTIL_DIR = "Util";
        internal const string ASP_CONTROLLER_DIR = "Web";
        internal const string REACT_PAGE_DIR = "pages";
        internal const string REACT_UTIL_DIR = "util";

        internal List<Func<string, string>> ConfigureServices { get; } = new List<Func<string, string>>();
        internal List<Func<string, string>> ConfigureServicesWhenWebServer { get; } = new List<Func<string, string>>();
        internal List<Func<string, string>> ConfigureServicesWhenBatchProcess { get; } = new List<Func<string, string>>();
        internal List<Func<string, string>> ConfigureWebApp { get; } = new List<Func<string, string>>();

        internal List<IReactPage> ReactPages { get; } = new List<IReactPage>();

        internal readonly Dictionary<GraphNode<Aggregate>, AggregateFile> _itemsByAggregate = new();
        internal void Aggregate(GraphNode<Aggregate> aggregate, Action<AggregateFile> fn) {
            if (!_itemsByAggregate.TryGetValue(aggregate, out var item)) {
                item = new AggregateFile(aggregate);
                _itemsByAggregate.Add(aggregate, item);
            }
            fn(item);
        }

        internal void GenerateCode(CodeRenderingContext context) {
            context.EditWebApiDirectory(genDir => {
                genDir.Generate(Configure.Render(
                    context,
                    ConfigureServicesWhenWebServer,
                    ConfigureWebApp,
                    ConfigureServicesWhenBatchProcess,
                    ConfigureServices));
                genDir.Generate(EnumDefs.Render(context));
                genDir.Generate(new ApplicationService().Render(context));

                genDir.Directory(ASP_UTIL_DIR, utilDir => {
                    utilDir.Generate(RuntimeSettings.Render(context));
                    utilDir.Generate(Parts.Utility.DotnetExtensions.Render(context));
                    utilDir.Generate(Parts.Utility.AggregateUpdateEvent.Render(context));
                    utilDir.Generate(Parts.Utility.FromTo.Render(context));
                    utilDir.Generate(Parts.Utility.UtilityClass.RenderJsonConversionMethods(context));
                });
                genDir.Directory("Web", controllerDir => {
                    controllerDir.Generate(MultiView.RenderCSharpSearchConditionBaseClass(context));
                });
                genDir.Directory("EntityFramework", efDir => {
                    var onModelCreating = _itemsByAggregate
                        .Where(x => x.Value.OnModelCreating.Any())
                        .Select(x => $"OnModelCreating_{x.Key.Item.ClassName}");
                    efDir.Generate(new DbContextClass(context.Config).RenderDeclaring(context, onModelCreating));
                });

                foreach (var aggFile in _itemsByAggregate.Values) {
                    genDir.Generate(aggFile.Render(context));
                }
            });

            context.EditReactDirectory(reactDir => {
                var reactProjectTemplate = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ApplicationTemplates", "REACT_AND_WEBAPI", "react");

                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "index.tsx"));
                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "nijo-default-style.css"));
                reactDir.Generate(TypesTsx.Render(context, _itemsByAggregate.Select(x => KeyValuePair.Create(x.Key, x.Value.TypeScriptDataTypes))));
                reactDir.Generate(MenuTsx.Render(context, ReactPages));

                reactDir.Directory("collection", layoutDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "collection");
                    foreach (var file in Directory.GetFiles(source)) layoutDir.CopyIfNotHandled(file);
                });
                reactDir.Directory("input", userInputDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "input");
                    foreach (var file in Directory.GetFiles(source)) userInputDir.CopyIfNotHandled(file);

                    // TODO: どの集約がコンボボックスを作るのかをNijoFeatureBaseに主導権握らせたい
                    userInputDir.Generate(ComboBox.RenderDeclaringFile(context));
                });
                reactDir.Directory("util", reactUtilDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "util");
                    foreach (var file in Directory.GetFiles(source)) reactUtilDir.CopyIfNotHandled(file);
                });
                reactDir.Directory(REACT_PAGE_DIR, pageDir => {
                    foreach (var group in ReactPages.GroupBy(p => p.DirNameInPageDir)) {
                        pageDir.Directory(group.Key, aggregatePageDir => {
                            foreach (var page in group) {
                                aggregatePageDir.Generate(page.GetSourceFile());
                            }
                        });
                    }
                });
            });
        }
    }
}
