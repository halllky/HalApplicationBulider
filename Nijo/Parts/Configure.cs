using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Features.Logging;
using Nijo.Parts.WebServer;

namespace Nijo.Parts {
    internal class Configure {
        internal const string ABSTRACT_CLASS_NAME = "DefaultConfiguration";
        internal const string CONCRETE_CLASS_NAME = "CustomizedConfiguration";

        internal const string CLASSNAME_WEBAPI = "DefaultConfigurationInWebApi";
        internal const string CLASSNAME_CLI = "DefaultConfigurationInCli";

        internal const string INIT_WEB_HOST_BUILDER = "InitWebHostBuilder";
        internal const string INIT_BATCH_PROCESS = "InitAsBatchProcess";
        internal const string CONFIGURE_SERVICES = "ConfigureServices";
        internal const string INIT_WEBAPPLICATION = "InitWebApplication";

        internal static string GetClassFullname(Config config) => $"{config.RootNamespace}.{ABSTRACT_CLASS_NAME}";

        internal static SourceFile RenderConfigureServices() {
            return new SourceFile {
                FileName = "DefaultConfiguration.cs",
                RenderContent = _ctx => {
                    var appSrv = new WebServer.ApplicationService();
                    var runtimeServerSettings = RuntimeSettings.ServerSetiingTypeFullName;

                    return $$"""
                        namespace {{_ctx.Config.RootNamespace}} {
                            using Microsoft.Extensions.Configuration;
                            using Microsoft.Extensions.DependencyInjection;
                            using Microsoft.Extensions.Logging;

                            public abstract class {{ABSTRACT_CLASS_NAME}} {

                                /// <summary>
                                /// DI設定
                                /// </summary>
                                public void {{CONFIGURE_SERVICES}}(IServiceCollection services) {

                                    // アプリケーションサービス
                                    services.AddScoped<{{appSrv.AbstractClassName}}, {{appSrv.ConcreteClassName}}>();
                                    ConfigureApplicationService(services);

                                    // 実行時設定ファイル
                                    ConfigureRuntimeSetting(services);

                                    // DB接続
                                    services.AddScoped<Microsoft.EntityFrameworkCore.DbContext, {{_ctx.Config.DbContextName}}>();
                                    ConfigureDbContext(services);

                                    // ログ
                                    ConfigureLogger(services);
                                }

                                /// <summary>
                                /// <see cref="{{appSrv.ConcreteClassName}}"/> をDIに登録します。
                                /// </summary>
                                protected virtual void ConfigureApplicationService(IServiceCollection services) {
                                    services.AddScoped<{{appSrv.ConcreteClassName}}>();
                                }

                                /// <summary>
                                /// 実行時設定をどこから参照するかの処理をDIに登録します。
                                /// <see cref="{{RuntimeSettings.ServerSetiingTypeFullName}}"/> 型を登録してください。
                                /// </summary>
                                protected virtual void ConfigureRuntimeSetting(IServiceCollection services) {
                                    services.AddScoped(provider => {
                                        // appsettings.json から読み取る
                                        var instance = {{runtimeServerSettings}}.{{RuntimeSettings.GET_DEFAULT}}();
                                        provider
                                            .GetRequiredService<IConfiguration>()
                                            .GetSection("{{RuntimeSettings.APP_SETTINGS_SECTION_NAME}}")
                                            .Bind(instance);
                                        return instance;
                                    });
                                }

                                /// <summary>
                                /// Entity Framework Core のDbContextをDIに登録します。
                                /// 既定ではSQLiteを使用します。
                                /// </summary>
                                protected virtual void ConfigureDbContext(IServiceCollection services) {
                                    services.AddDbContext<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>((provider, option) => {
                                        var setting = provider.GetRequiredService<{{runtimeServerSettings}}>();
                                        var connStr = setting.{{RuntimeSettings.GET_ACTIVE_CONNSTR}}();
                                        Microsoft.EntityFrameworkCore.ProxiesExtensions.UseLazyLoadingProxies(option);
                                        Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(option, connStr);
                                    });
                                }

                                /// <summary>
                                /// ログ出力の設定を行います。
                                /// <see cref="Microsoft.Extensions.Logging.ILogger"/> 型を登録してください。
                                /// </summary>
                                protected virtual void ConfigureLogger(IServiceCollection services) {
                                    services.AddScoped<ILogger>(provider => {
                                        var setting = provider.GetRequiredService<{{runtimeServerSettings}}>();
                                        return new {{DefaultLogger.CLASSNAME}}(setting.LogDirectory);
                                    });
                                }
                            }
                        }
                        """;
                },
            };
        }

        internal static SourceFile RenderWebapiConfigure() {
            return new SourceFile {
                FileName = "DefaultConfigurer.cs",
                RenderContent = _ctx => {
                    var app = new ApplicationService();

                    return $$"""
                        namespace {{_ctx.Config.RootNamespace}} {
                            using Microsoft.Extensions.DependencyInjection;
                            using Microsoft.Extensions.Logging;

                            internal static class {{CLASSNAME_WEBAPI}} {

                                /// <summary>
                                /// DI設定（Webアプリケーション特有のもの）
                                /// </summary>
                                internal static void {{INIT_WEB_HOST_BUILDER}}(this WebApplicationBuilder builder) {
                                    new {{app.ConcreteClassName}}.{{CONCRETE_CLASS_NAME}}().{{CONFIGURE_SERVICES}}(builder.Services);

                                    // HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)
                                    builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {
                                        options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);
                                    });

                                    // npm start で実行されるポートがASP.NETのそれと別なので

                                    builder.Services.AddCors(options => {
                                        options.AddDefaultPolicy(builder => {
                                            builder.WithOrigins("{{_ctx.GeneratedProject.ReactProject.GetDebuggingClientUrl().ToString().TrimEnd('/')}}")
                                                .AllowAnyMethod()
                                                .AllowAnyHeader()
                                                .AllowCredentials();
                                        });
                                    });

                                    builder.Services.AddControllers(option => {
                                        // エラーハンドリング
                                        option.Filters.Add<{{_ctx.Config.RootNamespace}}.HttpResponseExceptionFilter>();

                                    }).AddJsonOptions(option => {
                                        // JSON日本語設定
                                        {{Utility.UtilityClass.CLASSNAME}}.{{Utility.UtilityClass.MODIFY_JSONOPTION}}(option.JsonSerializerOptions);
                                    });
                                }

                                /// <summary>
                                /// Webサーバー起動時初期設定
                                /// </summary>
                                internal static void {{INIT_WEBAPPLICATION}}(this WebApplication app) {
                                    // 前述AddCorsの設定をするならこちらも必要
                                    app.UseCors();
                                }
                            }

                        }
                        """;
                },
            };
        }

        internal static SourceFile RenderCliConfigure() {
            return new SourceFile {
                FileName = "DefaultConfigurer.cs",
                RenderContent = _ctx => {
                    var app = new ApplicationService();

                    return $$"""
                        namespace {{_ctx.Config.RootNamespace}} {
                            using Microsoft.Extensions.DependencyInjection;
                            using Microsoft.Extensions.Logging;

                            internal static class {{CLASSNAME_CLI}} {
                                /// <summary>
                                /// バッチプロセス起動時初期設定
                                /// </summary>
                                internal static void {{INIT_BATCH_PROCESS}}(this IServiceCollection services) {
                                    new {{app.ConcreteClassName}}.{{CONCRETE_CLASS_NAME}}().{{CONFIGURE_SERVICES}}(services);
                                }
                            }

                        }
                        """;

                },
            };

        }
    }
}
