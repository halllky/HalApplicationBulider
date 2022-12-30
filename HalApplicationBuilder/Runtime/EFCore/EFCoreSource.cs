﻿using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.Runtime.EFCore {
    internal class EFCoreSource {
        internal Config Config { get; init; }
        internal Core.AggregateBuilder AggregateBuilder { get; init; }

        internal string TransformText() {
            var aggregates = AggregateBuilder.EnumerateAllAggregates();
            var template = new EFCoreSourceTemplate {
                DbContextName = Config.DbContextName,
                DbContextNamespace = Config.DbContextNamespace,
                EntityNamespace = Config.EntityNamespace,
                EntityClasses = aggregates.Select(a => a.ToDbTableModel()),
            };
            return template.TransformText();
        }
    }

    partial class EFCoreSourceTemplate {
        internal string DbContextName { get; set; }
        internal string DbContextNamespace { get; set; }
        internal string EntityNamespace { get; set; }
        internal IEnumerable<Core.AutoGenerateDbEntityClass> EntityClasses { get; set; }
    }
}
