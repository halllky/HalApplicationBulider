using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    public class ApplicationService : ISummarizedFile {
        public string AbstractClassName => $"AutoGeneratedApplicationService";
        public string ConcreteClassName => $"OverridedApplicationService";

        internal string ConcreteClassFileName => $"{ConcreteClassName}.cs";
        internal string AbstractClassFileName => $"{AbstractClassName}.cs";

        public string ServiceProvider = "ServiceProvider";
        public string DbContext = "DbContext";

        public const string CURRENT_TIME = "CurrentTime";
        public const string CURRENT_USER = "CurrentUser";

        // ----------------------------------------------
        /// <summary>
        /// アプリケーションサービスにメソッドを追加します。
        /// <see cref="CodeRenderingContext.UseSummarizedFile{T}()"/> 経由で取得したインスタンスに対して実行しないと反映されないので注意。
        /// </summary>
        internal void Add(string sourceCode) {
            _sourceCodes.Add(sourceCode);
        }
        private readonly List<string> _sourceCodes = new();

        int ISummarizedFile.RenderingOrder => 888; // 特に理由はないが、通常のファイルより後、enum定義などより前の順番で生成するとうまくいきそうなので888
        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {

            context.CoreLibrary.AutoGeneratedDir(dir => {
                dir.Generate(RenderToCoreLibrary());
            });
        }

        private SourceFile RenderToCoreLibrary() => new SourceFile {
            FileName = AbstractClassFileName,
            RenderContent = ctx => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.Extensions.DependencyInjection;
                    using {{ctx.Config.DbContextNamespace}};

                    public partial class {{AbstractClassName}} {
                        public {{AbstractClassName}}(IServiceProvider serviceProvider) {
                            {{ServiceProvider}} = serviceProvider;
                        }

                        public IServiceProvider {{ServiceProvider}} { get; }

                        private {{ctx.Config.DbContextName}}? _dbContext;
                        public virtual {{ctx.Config.DbContextName}} {{DbContext}} => _dbContext ??= {{ServiceProvider}}.GetRequiredService<{{ctx.Config.DbContextName}}>();

                        /// <summary>
                        /// <para>
                        /// 現在時刻。データ更新時の更新時刻の記録などに使用。
                        /// <see cref="DateTime.Now"/> を使ってしまうと現在時刻に依存する処理のテストが困難になるので、基本的にはこのプロパティを使うこと。
                        /// </para>
                        /// <para>
                        /// 同一のリクエストの中では、たとえ実時刻に多少のずれがあったとしてもすべて同じ時刻（リクエスト開始時点の時刻）になる。
                        /// 理由は、例えば深夜0時前後の処理で処置の途中で日付が変わることでロジックに影響が出る、などといった事象を防ぐため。
                        /// </para>
                        /// <para>
                        /// ちなみにログ出力の時刻にはこのプロパティが用いられず、正確な現在時刻が出力される。
                        /// </para>
                        /// </summary>
                        public virtual DateTime {{CURRENT_TIME}} => _currentTime ??= DateTime.Now;
                        private DateTime? _currentTime;

                        /// <summary>
                        /// 現在操作中のユーザーの名前。データ更新時の更新者の記録などに使用。
                        /// </summary>
                        public virtual string {{CURRENT_USER}} => _currentUser ??= "UNDEFINED";
                        private string? _currentUser;
                {{_sourceCodes.SelectTextTemplate(code => $$"""

                        {{WithIndent(code, "        ")}}
                """)}}
                    }
                }
                """,
        };

        internal string RenderConcreteClass(Config config) => $$"""
            using Microsoft.EntityFrameworkCore;

            namespace {{config.RootNamespace}} {
                /// <summary>
                /// 自動生成された検索機能や登録機能を上書きする場合はこのクラス内でそのメソッドやプロパティをoverrideしてください。
                /// </summary>
                public partial class {{ConcreteClassName}} : {{AbstractClassName}} {
                    public {{ConcreteClassName}}(IServiceProvider serviceProvider) : base(serviceProvider) { }
                }
            }
            """;
    }
}
