﻿using System;
namespace HalApplicationBuilder.Core.UIModel {
    public class SearchConditionBase {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public DotnetEx.Page GetPageObject() => new(Page, PageSize);
    }
}
