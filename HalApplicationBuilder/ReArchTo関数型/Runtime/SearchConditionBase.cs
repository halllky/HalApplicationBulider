﻿using System;
namespace HalApplicationBuilder.ReArchTo関数型.Runtime {
    public class SearchConditionBase {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public DotnetEx.Page GetPageObject() => new(Page, PageSize);
    }
}
