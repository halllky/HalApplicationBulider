﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder {
    public sealed class HalApp : IApplicationSchema, IDbSchema, IViewModelProvider {

        public static void Configure(IServiceCollection serviceCollection, Config config, Assembly assembly, string @namespace = null) {
            var rootAggregateTypes = assembly
                .GetTypes()
                .Where(type => type.GetCustomAttribute<AggregateAttribute>() != null);

            if (!string.IsNullOrWhiteSpace(@namespace)) {
                rootAggregateTypes = rootAggregateTypes.Where(type => type.Namespace == @namespace);
            }

            Configure(serviceCollection, config, rootAggregateTypes.ToArray());
        }
        public static void Configure(IServiceCollection serviceCollection, Config config, Type[] rootAggregateTypes) {
            serviceCollection.AddScoped(_ => config);
            serviceCollection.AddScoped(provider => new HalApp(rootAggregateTypes, provider));
            serviceCollection.AddScoped<IAggregateMemberFactory>(provider => new Core.Members.AggregateMemberFactory(provider));
            serviceCollection.AddScoped<IApplicationSchema>(provider => provider.GetRequiredService<HalApp>());
            serviceCollection.AddScoped<IViewModelProvider>(provider => provider.GetRequiredService<HalApp>());
            serviceCollection.AddScoped<IDbSchema>(provider => provider.GetRequiredService<HalApp>());
        }

        private HalApp(Type[] rootAggregateTypes, IServiceProvider serviceProvider) {
            _rootAggregateTypes = rootAggregateTypes;
            _services = serviceProvider;
        }

        private readonly Type[] _rootAggregateTypes;
        private readonly IServiceProvider _services;


        /// <summary>
        /// コードの自動生成を実行します。
        /// </summary>
        public void GenerateCode(TextWriter log = null) {

            var validator = new AggregateValidator(_services);
            if (validator.HasError(error => log?.WriteLine(error))) {
                log?.WriteLine("コード自動生成終了");
                return;
            }

            log?.WriteLine($"コード自動生成開始");

            var config = _services.GetRequiredService<Config>();

            var efSourceDir = Path.Combine(config.OutProjectDir, config.EntityFrameworkDirectoryRelativePath);
            if (!Directory.Exists(efSourceDir)) Directory.CreateDirectory(efSourceDir);

            log?.WriteLine("コード自動生成: Entity定義");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Entities.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(EntityFramework.EntityClassRenderer.Render(
                    _services.GetRequiredService<IApplicationSchema>(),
                    _services.GetRequiredService<IDbSchema>(),
                    config));
            }
            log?.WriteLine("コード自動生成: DbSet");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "DbSet.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(EntityFramework.DbSetRenderer.Render(
                    _services.GetRequiredService<IApplicationSchema>(),
                    _services.GetRequiredService<IDbSchema>(),
                    config));
            }
            log?.WriteLine("コード自動生成: OnModelCreating");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "OnModelCreating.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(EntityFramework.OnModelCreatingRenderer.Render(
                    _services.GetRequiredService<IApplicationSchema>(),
                    _services.GetRequiredService<IDbSchema>(),
                    config));
            }
            log?.WriteLine("コード自動生成: Search");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Search.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(EntityFramework.SearchMethodRenderer.Render(
                    _services.GetRequiredService<IApplicationSchema>(),
                    _services.GetRequiredService<IDbSchema>(),
                    _services.GetRequiredService<IViewModelProvider>(),
                    config));
            }
            log?.WriteLine("コード自動生成: AutoCompleteSource");
            using (var sw = new StreamWriter(Path.Combine(efSourceDir, "AutoCompleteSource.cs"), append: false, encoding: Encoding.UTF8)) {
                sw.Write(EntityFramework.AutoCompleteSourceMethodRenderer.Render(
                    _services.GetRequiredService<IApplicationSchema>(),
                    _services.GetRequiredService<IDbSchema>(),
                    _services.GetRequiredService<IViewModelProvider>(),
                    config));
            }

            log?.WriteLine("コード自動生成: MVC Model");
            var modelDir = Path.Combine(config.OutProjectDir, config.MvcModelDirectoryRelativePath);
            var modelFile = Path.Combine(modelDir, "Models.cs");
            if (!Directory.Exists(modelDir)) Directory.CreateDirectory(modelDir);
            using (var sw = new StreamWriter(modelFile, append: false, encoding: Encoding.UTF8)) {
                var source = new AspNetMvc.MvcModels();
                sw.Write(source.TransformText(
                    _services.GetRequiredService<IApplicationSchema>(),
                    _services.GetRequiredService<IViewModelProvider>(),
                    config));
            }

            //stream?.WriteLine("コード自動生成: MVC View - 既存ファイル削除");
            var viewDir = Path.Combine(config.OutProjectDir, config.MvcViewDirectoryRelativePath);
            if (!Directory.Exists(viewDir)) Directory.CreateDirectory(viewDir);
            //foreach (var file in Directory.GetFiles(viewDir)) {
            //    File.Delete(file);
            //}

            log?.WriteLine("コード自動生成: MVC View - MultiView");
            foreach (var aggregate in _services.GetRequiredService<IApplicationSchema>().RootAggregates()) {
                var view = new AspNetMvc.MultiView(aggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText(_services.GetRequiredService<IViewModelProvider>()));
            }

            log?.WriteLine("コード自動生成: MVC View - SingleView");
            foreach (var aggregate in _services.GetRequiredService<IApplicationSchema>().RootAggregates()) {
                var view = new AspNetMvc.SingleView(aggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText(
                    _services.GetRequiredService<IViewModelProvider>(),
                    config));
            }

            log?.WriteLine("コード自動生成: MVC View - CreateView");
            foreach (var aggregate in _services.GetRequiredService<IApplicationSchema>().RootAggregates()) {
                var view = new AspNetMvc.CreateView(aggregate);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText(
                    _services.GetRequiredService<IViewModelProvider>(),
                    config));
            }

            log?.WriteLine("コード自動生成: MVC View - 集約部分ビュー");
            foreach (var aggregate in _services.GetRequiredService<IApplicationSchema>().AllAggregates()) {
                var view = new AspNetMvc.InstancePartialView(
                    aggregate,
                    config);
                var filename = Path.Combine(viewDir, view.FileName);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText(
                    _services.GetRequiredService<IViewModelProvider>()));
            }

            log?.WriteLine("コード自動生成: MVC Controller");
            var controllerDir = Path.Combine(config.OutProjectDir, config.MvcControllerDirectoryRelativePath);
            var controllerFile = Path.Combine(controllerDir, "Controllers.cs");
            if (!Directory.Exists(controllerDir)) Directory.CreateDirectory(controllerDir);
            using (var sw = new StreamWriter(controllerFile, append: false, encoding: Encoding.UTF8)) {
                var source = new AspNetMvc.Controller();
                sw.Write(source.TransformText(
                    _services.GetRequiredService<IApplicationSchema>(),
                    _services.GetRequiredService<IViewModelProvider>(),
                    config));
            }

            log?.WriteLine("コード自動生成: JS");
            {
                var view = new AspNetMvc.JsTemplate();
                var filename = Path.Combine(viewDir, AspNetMvc.JsTemplate.FILE_NAME);
                using var sw = new StreamWriter(filename, append: false, encoding: Encoding.UTF8);
                sw.Write(view.TransformText());
            }

            log?.WriteLine("コード自動生成終了");
        }


        /// <summary>
        /// 実行時コンテキストを取得します。
        /// </summary>
        public Core.Runtime.RuntimeContext GetRuntimeContext(Assembly runtimeAssembly) {
            return new Core.Runtime.RuntimeContext(runtimeAssembly, _services);
        }


        #region ApplicationSchema
        private HashSet<Aggregate> _appSchema;
        private Dictionary<string, Aggregate> _pathMapping;
        private IReadOnlySet<Aggregate> AppSchema {
            get {
                if (_appSchema == null) BuildAggregates();
                return _appSchema;
            }
        }
        private IReadOnlyDictionary<string, Aggregate> PathMapping {
            get {
                if (_pathMapping == null) BuildAggregates();
                return _pathMapping;
            }
        }
        private void BuildAggregates() {
            var memberFactory = _services.GetRequiredService<IAggregateMemberFactory>();
            var rootAggregates = _rootAggregateTypes.Select(type => new Aggregate(type, null, memberFactory));

            _appSchema = new HashSet<Aggregate>();
            foreach (var aggregate in rootAggregates) {
                _appSchema.Add(aggregate);

                foreach (var descendant in aggregate.GetDescendants()) {
                    _appSchema.Add(descendant);
                }
            }

            _pathMapping = _appSchema
                .GroupBy(aggregate => new AggregatePath(aggregate))
                .ToDictionary(path => path.Key.Value, path => path.First());
        }

        IEnumerable<Aggregate> IApplicationSchema.AllAggregates() {
            return AppSchema;
        }
        IEnumerable<Aggregate> IApplicationSchema.RootAggregates() {
            return AppSchema.Where(a => a.Parent == null);
        }
        Aggregate IApplicationSchema.FindByTypeOrAggregateId(Type type, RefTargetIdAttribute aggregateId) {
            var foundByType = AppSchema.Where(a => a.UnderlyingType == type).ToArray();
            if (foundByType.Length <= 1)
                return foundByType.SingleOrDefault();
            else {
                if (aggregateId == null)
                    throw new InvalidOperationException(
                        $"There are several aggregates corresponding to type '{type.Name}'. " +
                        $"Please add {nameof(RefTargetIdAttribute)} to {nameof(RefTo<object>)} property " +
                        $"and add {nameof(AggregateIdAttribute)} to aggregate.");
                return AppSchema.SingleOrDefault(aggregate => aggregate.AggregateId?.Value == aggregateId.Value);
            }
        }
        Aggregate IApplicationSchema.FindByPath(string aggregatePath) {
            return PathMapping[aggregatePath];
        }
        #endregion ApplicationSchema


        #region DbSchema
        private Dictionary<Aggregate, DbEntity> _dbEntities;
        private IReadOnlyDictionary<Aggregate, DbEntity> DbEntities {
            get {
                if (_dbEntities == null) {
                    var config = _services.GetRequiredService<Config>();
                    var aggregates = ((IApplicationSchema)this).AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();
                    _dbEntities = new Dictionary<Aggregate, DbEntity>();
                    foreach (var aggregate in aggregates) {
                        var parent = aggregate.Parent == null
                            ? null
                            : _dbEntities[aggregate.Parent.Owner];
                        var child = new DbEntity(aggregate, parent, config);
                        _dbEntities.Add(aggregate, child);
                        parent?.children.Add(child);
                    }
                }
                return _dbEntities;
            }
        }

        DbEntity IDbSchema.GetDbEntity(Aggregate aggregate) {
            return DbEntities[aggregate];
        }
        #endregion DbSchema


        #region ViewModelProvider
        private Dictionary<Aggregate, SearchConditionClass> _searchConditions;
        private Dictionary<Aggregate, SearchResultClass> _searchResults;
        private Dictionary<Aggregate, MvcModel> _instanceModels;

        private IReadOnlyDictionary<Aggregate, SearchConditionClass> SearchConditions {
            get {
                if (_searchConditions == null) {

                    var config = _services.GetRequiredService<Config>();
                    var aggregates = ((IApplicationSchema)this).AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();

                    _searchConditions = new Dictionary<Aggregate, SearchConditionClass>();
                    foreach (var aggregate in aggregates) {
                        _searchConditions.Add(aggregate, new SearchConditionClass {
                            Source = aggregate,
                            Config = config,
                        });
                    }
                }
                return _searchConditions;
            }
        }
        private IReadOnlyDictionary<Aggregate, SearchResultClass> SearchResults {
            get {
                if (_searchResults == null) {

                    var config = _services.GetRequiredService<Config>();
                    var aggregates = ((IApplicationSchema)this).AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();

                    _searchResults = new Dictionary<Aggregate, SearchResultClass>();
                    foreach (var aggregate in aggregates) {
                        _searchResults.Add(aggregate, new SearchResultClass {
                            Source = aggregate,
                            Config = config,
                        });
                    }
                }
                return _searchResults;
            }
        }
        private IReadOnlyDictionary<Aggregate, MvcModel> InstanceModels {
            get {
                if (_instanceModels == null) {

                    var config = _services.GetRequiredService<Config>();
                    var aggregates = ((IApplicationSchema)this).AllAggregates()
                        .OrderBy(a => a.GetAncestors().Count())
                        .ToList();

                    _instanceModels = new Dictionary<Aggregate, MvcModel>();
                    foreach (var aggregate in aggregates) {
                        _instanceModels.Add(aggregate, new InstanceModelClass {
                            Source = aggregate,
                            Config = config,
                        });
                    }
                }
                return _instanceModels;
            }
        }

        MvcModel IViewModelProvider.GetInstanceModel(Aggregate aggregate) {
            return InstanceModels[aggregate];
        }
        SearchConditionClass IViewModelProvider.GetSearchConditionModel(Aggregate aggregate) {
            return SearchConditions[aggregate];
        }
        SearchResultClass IViewModelProvider.GetSearchResultModel(Aggregate aggregate) {
            return SearchResults[aggregate];
        }
        #endregion ViewModelProvider
    }
}
