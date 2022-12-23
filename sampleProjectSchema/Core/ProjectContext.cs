﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public class ProjectContext {
        public ProjectContext(string projectName, Assembly assembly) {
            ProjectName = projectName;
            _assembly = assembly;
        }

        public string ProjectName { get; }
        private readonly Assembly _assembly;

        private readonly Dictionary<Type, Aggregate> aggregates = new();

        public IEnumerable<Aggregate> EnumerateAllAggregates() {
            var rootAggregates = EnumerateRootAggregates();
            return rootAggregates.Union(rootAggregates.SelectMany(e => e.GetDescendantAggregates()));
        }
        public IEnumerable<Aggregate> EnumerateRootAggregates() {
            foreach (var type in _assembly.GetTypes()) {
                if (type.GetCustomAttribute<AggregateRootAttribute>() == null) continue;
                yield return GetOrCreateAggregate(type, null);
            }
        }


        internal Aggregate GetOrCreateAggregate(Type type, Aggregate parent, bool asChildren = false) {
            if (!aggregates.ContainsKey(type)) {
                aggregates.Add(type, new Aggregate(type, parent, this, asChildren: asChildren));
            }
            return aggregates[type];
        }
        internal Aggregate GetAggregate(Type type) {
            if (!aggregates.ContainsKey(type)) throw new InvalidOperationException($"{type.Name} のAggregateは未作成");
            return aggregates[type];
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

        public  object CreateInstance(Type type) {
            var instance = Activator.CreateInstance(type);
            var props = GetAggregate(type).GetProperties();
            foreach (var prop in props) {
                prop.UnderlyingPropInfo.SetValue(instance, prop.CreateInstanceDefaultValue());
            }
            return instance;
        }

        internal bool IsUserDefinedType(Type type) {
            return type.Assembly == _assembly;
        }
    }
}
