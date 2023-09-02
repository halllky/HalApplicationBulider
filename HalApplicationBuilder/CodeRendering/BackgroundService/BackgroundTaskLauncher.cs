using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.BackgroundService {
    internal class BackgroundTaskLauncher : TemplateBase {
        internal BackgroundTaskLauncher(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public override string FileName => "BackgroundTaskLauncher.cs";
        internal string ClassFullname => $"{_ctx.Config.RootNamespace}.{CLASSNAME}";
        private const string CLASSNAME = "BackgroundTaskLauncher";

        protected override string Template() {
            var dbContextFullName = $"{_ctx.Config.DbContextNamespace}.{_ctx.Config.DbContextName}";

            return $$"""
                using Microsoft.EntityFrameworkCore;
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Reflection;
                using System.Text;
                using System.Text.Json;
                using System.Text.Json.Serialization;
                using System.Threading.Tasks;

                namespace {{_ctx.Config.RootNamespace}} {
                    public sealed class {{CLASSNAME}} : Microsoft.Extensions.Hosting.BackgroundService {

                        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                            var serviceCollection = new ServiceCollection();
                            {{new Configure(_ctx).ClassFullname}}.{{Configure.INIT_BATCH_PROCESS}}(serviceCollection);
                            var services = serviceCollection.BuildServiceProvider();

                            var logger = services.GetRequiredService<ILogger>();
                            var settings = services.GetRequiredService<RuntimeSettings.Server>();
                            var runningTasks = new Dictionary<string, Task>();

                            var directory = Path.Combine(Directory.GetCurrentDirectory(), settings.JobDirectory ?? "job");
                            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                            stoppingToken.Register(() => {
                                logger.LogInformation($"バッチ起動監視処理の中止が要請されました。");
                            });

                            try {

                                logger.LogInformation($"バッチ起動監視 開始");

                                while (!stoppingToken.IsCancellationRequested) {
                                    // 待機
                                    try {
                                        await Task.Delay(settings.BackgroundTask.PollingSpanMilliSeconds, stoppingToken);
                                    } catch (TaskCanceledException) {
                                        continue;
                                    }

                                    // 終了しているバッチがないか調べる
                                    using var pollingScope = services.CreateScope();
                                    var dbContext = pollingScope.ServiceProvider.GetRequiredService<{{dbContextFullName}}>();
                                    DetectFinishing(runningTasks, dbContext, logger);

                                    // 起動対象バッチがあるかどうか検索
                                    var queued = dbContext
                                        .BackgroundTasks
                                        .Where(task => task.State == E_BackgroundTaskState.WaitToStart)
                                        .OrderBy(task => task.RequestTime)
                                        .Take(5)
                                        .ToArray();
                                    if (!queued.Any()) continue;

                                    // バッチ起動
                                    var now = DateTime.Now;
                                    var contextFactory = new BackgroundTaskContextFactory(now, services, directory);
                                    foreach (var entity in queued) {
                                        try {
                                            var backgroundTask = FindTaskByID(entity.BatchType);
                                            var executeArgument = CreateExecuteArgument(backgroundTask, entity, contextFactory, stoppingToken);

                                            var task = Task.Run(() => {
                                                logger.LogInformation("バッチ実行開始 {Id} ({Type} {Name})", entity.Id, entity.BatchType, entity.Name);
                                                backgroundTask.Execute(executeArgument);
                                                logger.LogInformation("バッチ実行終了 {Id} ({Type} {Name})", entity.Id, entity.BatchType, entity.Name);
                                            }, CancellationToken.None);
                                            runningTasks.Add(entity.Id, task);

                                            entity.StartTime = now;
                                            entity.State = E_BackgroundTaskState.Running;
                                            dbContext.SaveChanges();

                                        } catch (Exception ex) {
                                            logger.LogError(ex, "バッチの起動に失敗しました({Id} {Name}): {Message}", entity.Id, entity.Name, ex.Message);
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                logger.LogCritical(ex, "バッチ起動監視処理でエラーが発生しました: {Message}", ex.Message);
                            }

                            // 起動中ジョブの終了を待機
                            try {
                                logger.LogInformation("起動中ジョブの終了を待機します。");
                                using var disposingScope = services.CreateScope();
                                Task.WaitAll(runningTasks.Values.ToArray(), CancellationToken.None);
                                var dbContext = disposingScope.ServiceProvider.GetRequiredService<RDRA.EntityFramework.MyDbContext>();
                                DetectFinishing(runningTasks, dbContext, logger);
                            } catch (Exception ex) {
                                logger.LogCritical(ex, "バッチ起動監視処理(起動中ジョブの終了待機)でエラーが発生しました: {Message}", ex.Message);
                            }

                            logger.LogInformation($"バッチ起動監視 終了");
                        }

                        /// <summary>
                        /// バッチIDと対応するクラスのインスタンスを作成して返します。
                        /// </summary>
                        private BackgroundTask FindTaskByID(string batchType) {
                            var assembly = Assembly.GetExecutingAssembly();
                            var types = assembly
                                .GetTypes()
                                .Select(type => new { type, attr = type.GetCustomAttribute<BackgroundTaskAttribute>() })
                                .Where(x => x.attr?.Id == batchType)
                                .ToArray();
                            if (types.Length == 0)
                                throw new InvalidOperationException($"バッチID '{batchType}' と対応するバッチが見つかりません。");
                            if (types.Length >= 2)
                                throw new InvalidOperationException($"バッチID '{batchType}' と対応するバッチが複数見つかりました。");

                            var type = types[0].type;
                            var instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"バッチ {batchType}: '{type.Name}' クラスのインスタンス化に失敗しました。引数なしコンストラクタがあるか等確認してください。");
                            if (instance is not BackgroundTask backgroundTask) throw new InvalidOperationException($"バッチ {batchType}: '{type.Name}' クラスは{nameof(BackgroundTask)}クラスを継承している必要があります。");
                            return backgroundTask;
                        }

                        /// <summary>
                        /// ジョブの起動指示をもとに実行用のオブジェクトを作成して返します。
                        /// </summary>
                        private static JobChain CreateExecuteArgument(BackgroundTask backgroundTask, {{_ctx.Config.EntityNamespace}}.BackgroundTaskEntity entity, BackgroundTaskContextFactory contextFactory, CancellationToken stoppingToken) {
                            var type = backgroundTask.GetType();

                            while (type != null && type != typeof(object)) {
                                // パラメータなしの場合
                                if (type == typeof(BackgroundTask)) {
                                    return new JobChain(entity.Id, new(), contextFactory, stoppingToken);
                                }
                                // パラメータありの場合
                                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BackgroundTask<>)) {
                                    var genericType = type.GetGenericArguments()[0];

                                    object parsed;
                                    try {
                                        parsed = JsonSerializer.Deserialize(entity.ParameterJson, genericType)!;
                                    } catch (JsonException ex) {
                                        throw new InvalidOperationException($"バッチID {entity.Id} のパラメータを {genericType.Name} 型のJSONとして解釈できません。", ex);
                                    }

                                    var jobChainType = typeof(JobChainWithParameter<>).MakeGenericType(genericType);
                                    return (JobChain)Activator.CreateInstance(jobChainType, new object[] {
                                        entity.Id,
                                        parsed,
                                        new Stack<string>(),
                                        contextFactory,
                                        stoppingToken,
                                    })!;
                                }
                                type = type.BaseType;
                            }

                            throw new InvalidOperationException();
                        }

                        /// <summary>
                        /// 終了したタスクを検知して完了情報を記録します。
                        /// </summary>
                        private void DetectFinishing(Dictionary<string, Task> runningTasks, {{dbContextFullName}} dbContext, ILogger logger) {
                            // 終了したバッチを列挙
                            var completedTasks = runningTasks
                                .Where(kv => kv.Value.IsCompleted)
                                .ToDictionary(kv => kv.Key, kv => kv.Value);
                            if (!completedTasks.Any()) return;

                            // バッチと対応するデータをDBから検索
                            var ids = completedTasks.Keys.ToArray();
                            var entities = dbContext
                                .BackgroundTasks
                                .Where(e => ids.Contains(e.Id))
                                .ToDictionary(e => e.Id);
                            var list = completedTasks.ToDictionary(
                                kv => kv.Key,
                                kv => entities.GetValueOrDefault(kv.Key));

                            // そのバッチが完了した旨をDBに登録
                            var now = DateTime.Now;
                            foreach (var item in list) {
                                if (item.Value == null) {
                                    logger.LogError("タスク {Id} の完了情報の記録に失敗しました", item.Key);
                                    continue;
                                }
                                item.Value.FinishTime = now;
                                item.Value.State = completedTasks[item.Key].IsCompletedSuccessfully
                                    ? E_BackgroundTaskState.Success
                                    : E_BackgroundTaskState.Fault;
                                dbContext.SaveChanges();

                                runningTasks.Remove(item.Key);
                            }
                        }
                    }
                }

                namespace {{_ctx.Config.RootNamespace}} {
                    public enum E_BackgroundTaskState {
                        WaitToStart = 0,
                        Running = 1,
                        Success = 2,
                        Fault = 3,
                    }

                    public sealed class BackgroundTaskContextFactory {
                        public BackgroundTaskContextFactory(DateTime startTime, IServiceProvider serviceProvider, string directory) {
                            _startTime = startTime;
                            _serviceProvider = serviceProvider;
                            _directory = directory;
                        }
                        private readonly DateTime _startTime;
                        private readonly IServiceProvider _serviceProvider;
                        private readonly string _directory;

                        public BackgroundTaskContext CraeteScopedContext(string jobId) {
                            var scope = _serviceProvider.CreateScope();
                            var dirName = $"{_startTime:yyyyMMddHHmmss}_{jobId}";
                            var workingDirectory = Path.Combine(_directory, dirName);
                            return new BackgroundTaskContext(scope, _startTime, workingDirectory);
                        }
                        public BackgroundTaskContext<TParameter> CraeteScopedContext<TParameter>(string jobId, TParameter parameter) {
                            var scope = _serviceProvider.CreateScope();
                            var dirName = $"{_startTime:yyyyMMddHHmmss}_{jobId}";
                            var workingDirectory = Path.Combine(_directory, dirName);
                            return new BackgroundTaskContext<TParameter>(parameter, scope, _startTime, workingDirectory);
                        }
                    }

                    public class BackgroundTaskContext : IDisposable {
                        public BackgroundTaskContext(IServiceScope serviceScope, DateTime startTime, string workingDirectory) {
                            StartTime = startTime;
                            WorkingDirectory = workingDirectory;
                            _serviceScope = serviceScope;
                        }

                        private readonly IServiceScope _serviceScope;

                        public DateTime StartTime { get; }
                        public string WorkingDirectory { get; }

                        public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;
                        public ILogger Logger => ServiceProvider.GetRequiredService<ILogger>();
                        public RDRA.EntityFramework.MyDbContext DbContext => ServiceProvider.GetRequiredService<RDRA.EntityFramework.MyDbContext>();

                        void IDisposable.Dispose() {
                            _serviceScope.Dispose();
                        }
                    }
                    public class BackgroundTaskContext<TParameter> : BackgroundTaskContext {
                        public BackgroundTaskContext(TParameter parameter, IServiceScope serviceScope, DateTime startTime, string workingDirectory)
                            : base(serviceScope, startTime, workingDirectory) {
                            Parameter = parameter;
                        }
                        public TParameter Parameter { get; }
                    }

                    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                    sealed class BackgroundTaskAttribute : Attribute {
                        public BackgroundTaskAttribute(string id) {
                            Id = id;
                        }
                        public string Id { get; }
                        public string? DisplayName { get; set; }
                    }
                }

                namespace {{_ctx.Config.EntityNamespace}} {
                    public class BackgroundTaskEntity {
                        [JsonPropertyName("id")]
                        public string Id { get; set; } = string.Empty;
                        [JsonPropertyName("name")]
                        public string Name { get; set; } = string.Empty;
                        [JsonPropertyName("batchType")]
                        public string BatchType { get; set; } = string.Empty;
                        [JsonPropertyName("parameter")]
                        public string ParameterJson { get; set; } = string.Empty;
                        [JsonPropertyName("state")]
                        public E_BackgroundTaskState State { get; set; }
                        [JsonPropertyName("requestTime")]
                        public DateTime RequestTime { get; set; }
                        [JsonPropertyName("startTime")]
                        public DateTime? StartTime { get; set; }
                        [JsonPropertyName("finishTime")]
                        public DateTime? FinishTime { get; set; }

                        public static void OnModelCreating(ModelBuilder modelBuilder) {
                            modelBuilder.Entity<BackgroundTaskEntity>(e => {
                                e.HasKey(e => e.Id);
                            });
                        }
                    }
                }

                namespace {{_ctx.Config.DbContextNamespace}} {
                    partial class {{_ctx.Config.DbContextName}} {
                        public virtual DbSet<{{_ctx.Config.EntityNamespace}}.BackgroundTaskEntity> BackgroundTasks { get; set; }
                    }
                }
            
                """;
        }
    }
}
