﻿using System;
using System.Collections.Generic;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;

namespace HalApplicationBuilder.Core {
    public interface IAggregateMember {
        string Name { get; }

        Aggregate Owner { get; }
        IEnumerable<Aggregate> GetChildAggregates();

        bool IsPrimaryKey { get; }
        bool IsInstanceName { get; }
        int? InstanceNameOrder { get; }

        bool IsCollection { get; }

        IEnumerable<string> GetInvalidErrors();

        IEnumerable<DbColumn> ToDbColumnModel();

        internal void Accept(IMemberVisitor visitor);
    }
}
