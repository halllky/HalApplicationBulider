﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Props;
using haldoc.Schema;
using Microsoft.EntityFrameworkCore;

namespace haldoc.Core {
    public class ProjectContext {
        public ProjectContext(
            string projectName,
            Assembly assembly,
            Assembly webAssembly = null,
            DbContext dbContext = null) {
            ProjectName = projectName;
            SchemaAssembly = assembly;
            RuntimeAssembly = webAssembly;
            DbContext = dbContext;
        }

        public string ProjectName { get; }
        public Assembly SchemaAssembly { get; }
        public Assembly RuntimeAssembly { get; }
        public DbContext DbContext { get; }

        public string GetOutputNamespace(E_Namespace ns) {
            return ns switch {
                E_Namespace.DbContext => "haldoc",
                E_Namespace.DbEntity => "haldoc.AutoGenerated.Entities",
                E_Namespace.MvcModel => "haldoc.AutoGenerated.Mvc",
                _ => throw new ArgumentException(),
            };
        }

        private readonly Dictionary<Type, Aggregate> aggregates = new();

        public IEnumerable<Aggregate> EnumerateAllAggregates() {
            var rootAggregates = EnumerateRootAggregates();
            return rootAggregates.Union(rootAggregates.SelectMany(e => e.GetDescendantAggregates()));
        }
        public IEnumerable<Aggregate> EnumerateRootAggregates() {
            foreach (var type in SchemaAssembly.GetTypes()) {
                if (type.GetCustomAttribute<AggregateRootAttribute>() == null) continue;
                yield return GetOrCreateAggregate(type, null);
            }
        }


        internal Aggregate GetOrCreateAggregate(Type type, AggregatePropBase parent, bool asChildren = false) {
            if (!aggregates.ContainsKey(type)) {
                aggregates.Add(type, new Aggregate(type, parent, this, asChildren: asChildren));
            }
            return aggregates[type];
        }
        public Aggregate FindAggregate(Type type) {
            if (!aggregates.ContainsKey(type)) throw new InvalidOperationException($"{type.Name} のAggregateは未作成");
            return aggregates[type];
        }
        public Aggregate FindAggregate(Guid guid) {
            var agg = aggregates.Select(a => a.Value).SingleOrDefault(a => a.GUID == guid);
            if (agg == null) throw new InvalidOperationException($"{guid} のAggregateは未作成");
            return agg;
        }


        public IEnumerable<AggregatePropBase> GenerateProperties(Type type, Aggregate owner) {
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                if (prop.GetCustomAttribute<NotMappedAttribute>() != null) continue;

                if (PrimitiveProperty.IsPrimitive(prop.PropertyType)) {
                    yield return new PrimitiveProperty { UnderlyingPropInfo = prop, Owner = owner, Context = this };

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                    yield return new ChildrenProperty { UnderlyingPropInfo = prop, Owner = owner, Context = this };

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Child<>)) {

                    if (prop.PropertyType.GetGenericArguments()[0].IsAbstract
                        && prop.GetCustomAttributes<VariationAttribute>().Any())
                        yield return new VariationProperty { UnderlyingPropInfo = prop, Owner = owner, Context = this };
                    else
                        yield return new ChildProperty { UnderlyingPropInfo = prop, Owner = owner, Context = this };

                } else if (prop.PropertyType.IsClass && IsUserDefinedType(prop.PropertyType)) {
                    yield return new ReferenceProperty { UnderlyingPropInfo = prop, Owner = owner, Context = this };
                }
            }
        }

        //public  object CreateInstance(Type type) {
        //    var instance = Activator.CreateInstance(type);
        //    var props = GetAggregate(type).GetProperties();
        //    foreach (var prop in props) {
        //        prop.UnderlyingPropInfo.SetValue(instance, prop.CreateInstanceDefaultValue());
        //    }
        //    return instance;
        //}

        internal bool IsUserDefinedType(Type type) {
            return type.Assembly == SchemaAssembly;
        }

        public Runtime.DynamicActionResult MapToListView(Aggregate aggregate) {
            if (RuntimeAssembly == null) return null;

            var searchConditionType = RuntimeAssembly.GetType($"{GetOutputNamespace(E_Namespace.MvcModel)}.{aggregate.ToSearchConditionModel().ClassName}");
            var listItemType = RuntimeAssembly.GetType($"{GetOutputNamespace(E_Namespace.MvcModel)}.{aggregate.ToListItemModel().ClassName}");
            var modelType = typeof(Runtime.ListViewModel<,>).MakeGenericType(searchConditionType, listItemType);

            var searchCondition = Activator.CreateInstance(searchConditionType);
            var model = Activator.CreateInstance(modelType);
            modelType.GetProperty(nameof(Runtime.ListViewModel.Filter), typeof(object)).SetValue(model, searchCondition);

            return new Runtime.DynamicActionResult {
                View = $"~/Views/__AutoGenerated/{aggregate.Name}__ListView.cshtml",
                Model = model,
            };
        }
        public Runtime.DynamicActionResult MapToCreateView(Aggregate aggregate) {
            if (RuntimeAssembly == null) return null;

            var instanceType = RuntimeAssembly.GetType($"{GetOutputNamespace(E_Namespace.MvcModel)}.{aggregate.ToSingleItemModel().ClassName}");
            var instance = Activator.CreateInstance(instanceType);

            var modelType = typeof(Runtime.SingleViewModel<>).MakeGenericType(instanceType);
            var model = Activator.CreateInstance(modelType);
            modelType.GetProperty(nameof(Runtime.SingleViewModel.Instance), typeof(object)).SetValue(model, instance);

            return new Runtime.DynamicActionResult {
                View = $"~/Views/__AutoGenerated/{aggregate.Name}__CreateView.cshtml",
                Model = model,
            };
        }

        public Runtime.DynamicActionResult SaveNewInstance(Aggregate aggregate, Runtime.SingleViewModel model) {

            var entities = aggregate.TransformMvcModelToDbEntities(model.Instance).ToArray();
            DbContext.AddRange(entities);
            DbContext.SaveChanges();

            return new Runtime.DynamicActionResult {
                View = $"~/Views/__AutoGenerated/{aggregate.Name}__CreateView.cshtml",
                Model = model.Instance,
            };
        }
    }

    /// <summary>
    /// コード生成先の名前空間の種類
    /// </summary>
    public enum E_Namespace {
        DbContext,
        DbEntity,
        MvcModel,
    }
}
