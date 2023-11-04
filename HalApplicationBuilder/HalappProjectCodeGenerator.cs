using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.EFCore;
using HalApplicationBuilder.CodeRendering.InstanceHandling;
using HalApplicationBuilder.CodeRendering.WebClient;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder {
    public sealed class HalappProjectCodeGenerator {
        internal HalappProjectCodeGenerator(HalappProject project, ILogger? log) {
            _project = project;
            _log = log;
        }

        private readonly HalappProject _project;
        private readonly ILogger? _log;

        /// <summary>
        /// dotnet new コマンドを実行します。
        /// </summary>
        internal HalappProjectCodeGenerator DotnetNew() {
            _log?.LogInformation($"プロジェクトを作成します。");

            var config = _project.ReadConfig();
            _project.Terminal.Run(new[] { "dotnet", "new", "webapi", "--output", ".", "--name", config.ApplicationName }, CancellationToken.None).Wait();

            // Create .gitignore file
            _project.Terminal.Run(new[] { "dotnet", "new", "gitignore" }, CancellationToken.None).Wait();

            var filename = Path.Combine(_project.ProjectRoot, ".gitignore");
            var gitignore = File.ReadAllLines(filename).ToList();
            gitignore.Insert(0, "# HalApplicationBuilder");
            File.WriteAllLines(filename, gitignore);

            return this;
        }
        /// <summary>
        /// Program.cs ファイルを編集し、必要なソースコードを追記します。
        /// </summary>
        /// <returns></returns>
        internal HalappProjectCodeGenerator EditProgramCs() {
            _log?.LogInformation($"Program.cs ファイルを書き換えます。");
            var config = _project.ReadConfig();
            var appSchema = _project.BuildSchema();
            var ctx = new CodeRenderingContext {
                Config = config,
                Schema = appSchema,
            };
            var programCsPath = Path.Combine(_project.ProjectRoot, "Program.cs");
            var lines = File.ReadAllLines(programCsPath).ToList();
            var regex1 = new Regex(@"^.*[a-zA-Z]+ builder = .+;$");
            var position1 = lines.FindIndex(regex1.IsMatch);
            if (position1 == -1) throw new InvalidOperationException("Program.cs の中にIServiceCollectionを持つオブジェクトを初期化する行が見つかりません。");

            lines.InsertRange(position1 + 1, new[] {
                $"",
                $"{new Configure(ctx).ClassFullname}.{Configure.INIT_WEB_HOST_BUILDER}(builder);",
                $"",
            });

            var regex2 = new Regex(@"^.*[a-zA-Z]+ app = .+;$");
            var position2 = lines.FindIndex(regex2.IsMatch);
            if (position2 == -1) throw new InvalidOperationException("Program.cs の中にappオブジェクトを初期化する行が見つかりません。");
            lines.InsertRange(position2 + 1, new[] {
                $"",
                $"{new Configure(ctx).ClassFullname}.{Configure.INIT_WEBAPPLICATION}(app);",
                $"",
            });
            File.WriteAllLines(programCsPath, lines);

            return this;
        }
        /// <summary>
        /// コードの自動生成を行います。
        /// </summary>
        /// <param name="log">ログ出力先</param>
        public HalappProjectCodeGenerator UpdateAutoGeneratedCode() {
            if (!_project.IsValidDirectory()) return this;

            _log?.LogInformation($"コード自動生成開始: {_project.ProjectRoot}");

            var config = _project.ReadConfig();
            var appSchema = _project.BuildSchema();
            var ctx = new CodeRenderingContext {
                Config = config,
                Schema = appSchema,
            };

            DirectorySetupper.StartSetup(_project.ProjectRoot, dir => {

                dir.Directory("__AutoGenerated", genDir => {
                    genDir.Generate(new Configure(ctx));
                    genDir.Generate(new EnumDefs(appSchema.EnumDefinitions, ctx));

                    foreach (var aggregate in ctx.Schema.RootAggregates()) {
                        genDir.Generate(new AggregateRenderer(aggregate, ctx));
                    }

                    genDir.Directory("Util", utilDir => {
                        utilDir.Generate(new CodeRendering.Util.RuntimeSettings(ctx));
                        utilDir.Generate(new CodeRendering.Util.DotnetExtensions(ctx.Config));
                        utilDir.Generate(new CodeRendering.Util.FromTo(ctx.Config));
                        utilDir.Generate(new CodeRendering.Logging.HttpResponseExceptionFilter(ctx.Config.RootNamespace));
                        utilDir.Generate(new CodeRendering.Logging.DefaultLogger(ctx.Config.RootNamespace));
                        var util = new CodeRendering.Util.Utility(ctx);
                        utilDir.Generate("JsonConversion.cs", util.RenderJsonConversionMethods());
                        utilDir.DeleteOtherFiles();
                    });
                    genDir.Directory("Web", controllerDir => {
                        controllerDir.Generate(CodeRendering.Searching.SearchFeature.CreateSearchConditionBaseClassTemplate(ctx));
                        controllerDir.Generate(new DebuggerController(ctx));
                        controllerDir.DeleteOtherFiles();
                    });
                    genDir.Directory("EntityFramework", efDir => {
                        efDir.Generate(new DbContext(ctx));
                        efDir.DeleteOtherFiles();
                    });
                    genDir.Directory("BackgroundService", bsDir => {
                        bsDir.Generate(new CodeRendering.BackgroundService.BackgroundTaskLauncher(ctx));
                        bsDir.Generate(new CodeRendering.BackgroundService.BackgroundTask { Context = ctx });

                        var bgTaskSearch = CodeRendering.BackgroundService.BackgroundTaskEntity.CreateSearchFeature(appSchema.Graph, ctx);
                        bsDir.Generate("BackgroundTaskController.cs", bgTaskSearch.RenderControllerAction());
                        bsDir.Generate("BackgroundTaskSearchClass.cs", bgTaskSearch.RenderCSharpClassDef());
                        bsDir.Generate("BackgroundTaskDbContextSearch.cs", bgTaskSearch.RenderDbContextMethod());

                        bsDir.DeleteOtherFiles();
                    });
                    genDir.DeleteOtherFiles();
                });
            });

            var reactProjectTemplate = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "client");
            if (!Directory.Exists(_project.WebClientProjectRoot)) {
                DotnetEx.IO.CopyDirectory(reactProjectTemplate, _project.WebClientProjectRoot);
            }

            DirectorySetupper.StartSetup(Path.Combine(_project.WebClientProjectRoot, "src", "__autoGenerated"), reactDir => {
                const string REACT_PAGE_DIR = "pages";
                string GetAggDirName(GraphNode<Aggregate> a) => a.Item.DisplayName.ToFileNameSafe();

                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "halapp.css"));
                reactDir.Generate(new index(ctx, a => $"{REACT_PAGE_DIR}/{GetAggDirName(a)}"));
                reactDir.Generate(new types(ctx));

                reactDir.Directory("application", reactApplicationDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "application");
                    foreach (var file in Directory.GetFiles(source)) reactApplicationDir.CopyFrom(file);
                    reactApplicationDir.DeleteOtherFiles();
                });
                reactDir.Directory("decoration", decorationDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "decoration");
                    foreach (var file in Directory.GetFiles(source)) decorationDir.CopyFrom(file);
                    decorationDir.DeleteOtherFiles();
                });
                reactDir.Directory("layout", layoutDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "layout");
                    foreach (var file in Directory.GetFiles(source)) layoutDir.CopyFrom(file);
                    layoutDir.DeleteOtherFiles();
                });
                reactDir.Directory("user-input", userInputDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "user-input");
                    foreach (var file in Directory.GetFiles(source)) userInputDir.CopyFrom(file);
                    userInputDir.DeleteOtherFiles();

                    userInputDir.Generate("AggregateComboBox.tsx", CodeRendering.KeywordSearching.ComboBox.RenderDeclaringFile(ctx.Schema.AllAggregates()));
                });
                reactDir.Directory("util", reactUtilDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "util");
                    foreach (var file in Directory.GetFiles(source)) reactUtilDir.CopyFrom(file);
                    reactUtilDir.DeleteOtherFiles();
                });
                reactDir.Directory(REACT_PAGE_DIR, pageDir => {
                    foreach (var root in ctx.Schema.RootAggregates()) {
                        pageDir.Directory(GetAggDirName(root), aggregateDir => {
                            aggregateDir.Generate(new CodeRendering.Searching.SearchFeature(root.As<IEFCoreEntity>(), ctx).CreateReactPage());
                            aggregateDir.Generate(new SingleView(root, ctx, SingleView.E_Type.Create));
                            aggregateDir.Generate(new SingleView(root, ctx, SingleView.E_Type.View));
                            aggregateDir.Generate(new SingleView(root, ctx, SingleView.E_Type.Edit));
                            aggregateDir.DeleteOtherFiles();
                        });
                    }

                    pageDir.Directory("BackgroundTask", bgTaskDir => {
                        var bgTaskSearch = CodeRendering.BackgroundService.BackgroundTaskEntity.CreateSearchFeature(appSchema.Graph, ctx);
                        bgTaskDir.Generate(bgTaskSearch.CreateReactPage());
                    });

                    pageDir.DeleteOtherFiles();
                });
                reactDir.DeleteOtherFiles();
            });

            _log?.LogInformation($"コード自動生成終了: {_project.ProjectRoot}");
            return this;
        }
        /// <summary>
        /// 必要なNuGetパッケージを参照に加えます。
        /// </summary>
        internal HalappProjectCodeGenerator AddNugetPackages() {

            _log?.LogInformation($"Microsoft.EntityFrameworkCore パッケージへの参照を追加します。");
            _project.Terminal.Run(new[] { "dotnet", "add", "package", "Microsoft.EntityFrameworkCore" }, CancellationToken.None).Wait();

            _log?.LogInformation($"Microsoft.EntityFrameworkCore.Proxies パッケージへの参照を追加します。");
            _project.Terminal.Run(new[] { "dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Proxies" }, CancellationToken.None).Wait();

            _log?.LogInformation($"Microsoft.EntityFrameworkCore.Design パッケージへの参照を追加します。"); // migration add に必要
            _project.Terminal.Run(new[] { "dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Design" }, CancellationToken.None).Wait();

            _log?.LogInformation($"Microsoft.EntityFrameworkCore.Sqlite パッケージへの参照を追加します。");
            _project.Terminal.Run(new[] { "dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Sqlite" }, CancellationToken.None).Wait();

            return this;
        }
        /// <summary>
        /// halapp.xml が無い場合作成します。
        /// </summary>
        internal HalappProjectCodeGenerator EnsureCreateHalappXml(string applicationName) {
            var xmlPath = _project.SchemaXml.GetPath();

            if (!File.Exists(xmlPath)) {
                var rootNamespace = applicationName.ToCSharpSafe();
                var config = new Config {
                    ApplicationName = applicationName,
                    DbContextName = "MyDbContext",
                };
                var xmlContent = new XDocument(config.ToXmlWithRoot());
                using var sw = new StreamWriter(xmlPath, append: false, encoding: new UTF8Encoding(false));
                sw.WriteLine(xmlContent.ToString());
            }

            return this;
        }

        private class DirectorySetupper {
            internal static void StartSetup(string absolutePath, Action<DirectorySetupper> fn) {
                var setupper = new DirectorySetupper(absolutePath);
                setupper.Directory("", fn);
            }
            private DirectorySetupper(string path) {
                Path = path;
                _generated = new HashSet<string>();
            }

            internal string Path { get; }

            private readonly HashSet<string> _generated;
            internal void Directory(string relativePath, Action<DirectorySetupper> fn) {
                var fullpath = System.IO.Path.Combine(Path, relativePath);
                if (!System.IO.Directory.Exists(fullpath))
                    System.IO.Directory.CreateDirectory(fullpath);

                _generated.Add(fullpath);

                fn(new DirectorySetupper(System.IO.Path.Combine(Path, relativePath)));
            }

            internal void Generate(string filename, string content) {
                var file = System.IO.Path.Combine(Path, filename);

                _generated.Add(file);

                using var sw = new StreamWriter(file, append: false, encoding: GetEncoding(file));
                sw.WriteLine(content);
            }
            internal void Generate(ITemplate template) {
                Generate(template.FileName, template.TransformText());
            }
            internal void CopyFrom(string copySourceFile) {
                var copyTargetFile = System.IO.Path.Combine(Path, System.IO.Path.GetFileName(copySourceFile));

                _generated.Add(copyTargetFile);

                var encoding = GetEncoding(copySourceFile);
                using var reader = new StreamReader(copySourceFile, encoding);
                using var writer = new StreamWriter(copyTargetFile, append: false, encoding: encoding);
                while (!reader.EndOfStream) {
                    writer.WriteLine(reader.ReadLine());
                }
            }
            internal void DeleteOtherFiles() {
                var deleteFiles = System.IO.Directory
                    .GetFiles(Path)
                    .Where(path => !_generated.Contains(path));
                foreach (var file in deleteFiles) {
                    if (!File.Exists(file)) continue;
                    File.Delete(file);
                }
                var deletedDirectories = System.IO.Directory
                    .GetDirectories(Path)
                    .Where(path => !_generated.Contains(path));
                foreach (var dir in deletedDirectories) {
                    if (!System.IO.Directory.Exists(dir)) continue;
                    System.IO.Directory.Delete(dir, true);
                }
            }

            private static Encoding GetEncoding(string filepath) {
                return System.IO.Path.GetExtension(filepath).ToLower() == "cs"
                    ? Encoding.UTF8 // With BOM
                    : new UTF8Encoding(false);
            }
        }
    }
}
