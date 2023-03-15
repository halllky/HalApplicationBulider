﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering.AspNetMvc {
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System;
    
    
    public partial class MvcMultiViewTemplate : MvcMultiViewTemplateBase {
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            this.Write("\n@model ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_modelTypeFullname));
            this.Write(@";
@{
    // ViewData[""Title""] = ;
}

<div class=""flex gap-3 items-center"">
    <h1 class=""font-bold text-[18px] select-none"">
        <!-- PageTitle -->
    </h1>
    <a asp-action=""New"" class=""halapp-btn-link"">新規作成</a>
</div>

<form>
    @* 検索条件欄 *@
    <div class=""border mt-2 p-2"">

");
 PushIndent("        "); 
 _rootAggregate.RenderSearchCondition(new RenderingContext(this)); 
 PopIndent(); 
            this.Write("\n        <button asp-action=\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(MvcControllerTemplate.SEARCH_ACTION_NAME));
            this.Write("\" class=\"halapp-btn-primary\">検索</button>\n        <button asp-action=\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(MvcControllerTemplate.CLEAR_ACTION_NAME));
            this.Write(@""" class=""halapp-btn-secondary"">クリア</button>
    </div>
    
    @* 検索結果欄 *@
    <div class=""mt-2"">
        <div style=""display: flex; justify-content: flex-end"">
        </div>
        <table class=""table table-sm text-left w-full border"">
            <thead class=""border-b"">
                <tr>
                    <th></th>
");
 var searchResult = _rootAggregate.ToSearchResultClass(_config); 
 foreach (var prop in searchResult.Properties) { 
            this.Write("                    <th>\n                        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            this.Write("\n                    </th>\n");
 } 
            this.Write("                </tr>\n            </thead>\n            <tbody>\n                @f" +
                    "or (int i = 0; i < Model.SearchResult.Count; i++)\n                {\n            " +
                    "        <tr>\n                        <td>\n                            <a asp-act" +
                    "ion=\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(MvcControllerTemplate.LINK_TO_SINGLE_VIEW_ACTION_NAME));
            this.Write("\"\n                               asp-route-id=\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(BoundIdPropertyPathName));
            this.Write("\"\n                               class=\"halapp-btn-link\">\n                       " +
                    "         詳細\n                            </a>\n                        </td>\n");
 foreach (var prop in searchResult.Properties) { 
            this.Write("                        <td>\n                            @Model.SearchResult[i].");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            this.Write("\n                        </td>\n");
 } 
            this.Write("                    </tr>\n                }\n            </tbody>\n        </table>" +
                    "\n    </div>\n</form>\n\n");
            return this.GenerationEnvironment.ToString();
        }
        
        public virtual void Initialize() {
        }
    }
    
    public class MvcMultiViewTemplateBase {
        
        private global::System.Text.StringBuilder builder;
        
        private global::System.Collections.Generic.IDictionary<string, object> session;
        
        private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        
        private string currentIndent = string.Empty;
        
        private global::System.Collections.Generic.Stack<int> indents;
        
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }
        
        public global::System.Text.StringBuilder GenerationEnvironment {
            get {
                if ((this.builder == null)) {
                    this.builder = new global::System.Text.StringBuilder();
                }
                return this.builder;
            }
            set {
                this.builder = value;
            }
        }
        
        protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors {
            get {
                if ((this.errors == null)) {
                    this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errors;
            }
        }
        
        public string CurrentIndent {
            get {
                return this.currentIndent;
            }
        }
        
        private global::System.Collections.Generic.Stack<int> Indents {
            get {
                if ((this.indents == null)) {
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                }
                return this.indents;
            }
        }
        
        public ToStringInstanceHelper ToStringHelper {
            get {
                return this._toStringHelper;
            }
        }
        
        public void Error(string message) {
            this.Errors.Add(new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message));
        }
        
        public void Warning(string message) {
            global::System.CodeDom.Compiler.CompilerError val = new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }
        
        public string PopIndent() {
            if ((this.Indents.Count == 0)) {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }
        
        public void PushIndent(string indent) {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }
        
        public void ClearIndent() {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }
        
        public void Write(string textToAppend) {
            this.GenerationEnvironment.Append(textToAppend);
        }
        
        public void Write(string format, params object[] args) {
            this.GenerationEnvironment.AppendFormat(format, args);
        }
        
        public void WriteLine(string textToAppend) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }
        
        public void WriteLine(string format, params object[] args) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }
        
        public class ToStringInstanceHelper {
            
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            
            public global::System.IFormatProvider FormatProvider {
                get {
                    return this.formatProvider;
                }
                set {
                    if ((value != null)) {
                        this.formatProvider = value;
                    }
                }
            }
            
            public string ToStringWithCulture(object objectToConvert) {
                if ((objectToConvert == null)) {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type)) {
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                global::System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new global::System.Type[] {
                            iConvertibleType});
                if ((methInfo != null)) {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}
