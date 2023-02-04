﻿using System;
namespace HalApplicationBuilder.Core {
    public class Config {
        public string OutProjectDir { get; init; }

        public string EntityFrameworkDirectoryRelativePath { get; init; }
        public string EntityNamespace { get; init; }
        public string DbContextNamespace { get; init; }
        public string DbContextName { get; init; }

        public string MvcControllerDirectoryRelativePath { get; init; }
        public string MvcControllerNamespace { get; init; }

        public string MvcModelDirectoryRelativePath { get; init; }
        public string MvcModelNamespace { get; init; }

        public string MvcViewDirectoryRelativePath { get; init; }
    }
}
