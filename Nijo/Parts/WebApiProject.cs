using Microsoft.Build.Evaluation;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nijo.Parts {
    /// <summary>
    /// 自動生成される ASP.NET Core API プロジェクトに対する操作を提供します。
    /// </summary>
    public class WebApiProject {

        public WebApiProject(GeneratedProject generatedProject) {
            _generatedProject = generatedProject;
        }

        private readonly GeneratedProject _generatedProject;

        public string ProjectRoot => Path.Combine(_generatedProject.SolutionRoot, "webapi");
        public string AutoGeneratedDir => Path.Combine(ProjectRoot, "__AutoGenerated");

        /// <summary>
        /// プロジェクトディレクトリを新規作成します。
        /// </summary>
        public void CreateProjectIfNotExists(Core.Config config) {
            if (Directory.Exists(ProjectRoot)) return;

            // 埋め込みリソースからテンプレートを出力
            var resources = new EmbeddedResource.Collection(Assembly.GetExecutingAssembly());
            foreach (var resource in resources.Enumerate("webapi")) {
                var destination = Path.Combine(
                    ProjectRoot,
                    Path.GetRelativePath("webapi", resource.RelativePath));

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                using var reader = resource.GetStreamReader();
                using var writer = SourceFile.GetStreamWriter(destination);
                while (!reader.EndOfStream) {
                    writer.WriteLine(reader.ReadLine());
                }
            }

            // ファイル名変更
            var beforeCsproj = Path.Combine(ProjectRoot, "NIJO_APPLICATION_TEMPLATE_WebApi.csproj");
            var afterCsproj = Path.Combine(ProjectRoot, $"{config.ApplicationName}_WebApi.csproj");
            File.Move(beforeCsproj, afterCsproj);

            // ソースコード中にあるテンプレートプロジェクトの文字列を置換
            var programCs = Path.Combine(ProjectRoot, "Program.cs");

            foreach (var file in new[] { afterCsproj, programCs }) {
                var beforeReplace = File.ReadAllText(file);
                var afterReplace = beforeReplace.Replace("NIJO_APPLICATION_TEMPLATE", config.RootNamespace);
                File.WriteAllText(file, afterReplace);
            }
        }

        /// <summary>
        /// デバッグ時に起動されるアプリケーションのURLを返します。
        /// </summary>
        public Uri GetDebugUrl() {
            return new Uri(GetDebuggingServerUrl().Split(';')[0]);
        }

        /// <summary>
        /// デバッグ時に起動されるSwagger UIのURLを返します。
        /// </summary>
        /// <returns></returns>
        public Uri GetSwaggerUrl() {
            return new Uri(new Uri(GetDebuggingServerUrl().Split(';')[0]), "swagger");
        }

        /// <summary>
        /// デバッグ用サーバーのURLを返します。
        /// </summary>
        private string GetDebuggingServerUrl() {

            // launchSettings.jsonのhttpsプロファイルのapplicationUrlセクションの値を読み取る
            var properties = Path.Combine(ProjectRoot, "Properties");
            if (!Directory.Exists(properties)) throw new DirectoryNotFoundException(properties);
            var launchSettings = Path.Combine(properties, "launchSettings.json");
            if (!File.Exists(launchSettings)) throw new FileNotFoundException(launchSettings);

            var json = File.ReadAllText(launchSettings);
            var obj = JsonSerializer.Deserialize<JsonObject>(json);
            if (obj == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (!obj.TryGetPropertyValue("profiles", out var profiles))
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (profiles == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (!profiles.AsObject().TryGetPropertyValue("https", out var https))
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (https == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (!https.AsObject().TryGetPropertyValue("applicationUrl", out var applicationUrl))
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (applicationUrl == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");

            return applicationUrl.GetValue<string>();
        }

        /// <summary>
        /// コード自動生成処理のソースをわかりやすくするためのクラス
        /// </summary>
        public class DirectoryEditor {
            public DirectoryEditor(CodeRenderingContext context, WebApiProject project) {
                _context = context;
                _project = project;
                _autoGeneratedDir = DirectorySetupper.StartSetup(_context, _project.AutoGeneratedDir);
            }
            private readonly CodeRenderingContext _context;
            private readonly WebApiProject _project;
            private readonly DirectorySetupper _autoGeneratedDir;

            /// <summary>
            /// 自動生成ディレクトリ直下へのソース生成を行います。
            /// </summary>
            public void AutoGeneratedDir(Action<DirectorySetupper> setup) {
                setup(_autoGeneratedDir);
            }
            /// <summary>
            /// ユーティリティに関するクラスを格納するディレクトリへのソース生成を行います。
            /// </summary>
            public void UtilDir(Action<DirectorySetupper> setup) {
                _autoGeneratedDir.Directory("Util", setup);
            }
            /// <summary>
            /// 
            /// </summary>
            public void ControllerDir(Action<DirectorySetupper> setup) {
                _autoGeneratedDir.Directory("Web", setup);
            }

            /// <summary>
            /// DI設定
            /// </summary>
            public List<Func<string, string>> ConfigureServices { get; } = new List<Func<string, string>>();
            /// <summary>
            /// Webサーバーが起動するタイミングで実行されるアプリケーション設定処理
            /// </summary>
            public List<Func<string, string>> ConfigureWebApp { get; } = new List<Func<string, string>>();
        }
    }
}
