﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン: 17.0.0.0
//  
//     このファイルへの変更は、正しくない動作の原因になる可能性があり、
//     コードが再生成されると失われます。
// </auto-generated>
// ------------------------------------------------------------------------------
namespace HalApplicationBuilder.CodeRendering.WebClient
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using HalApplicationBuilder.Core;
    using HalApplicationBuilder.CodeRendering.Presentation;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class SingleView : SingleViewBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(@"import { useState, useCallback } from 'react';
import { useAppContext } from '../../hooks/AppContext';
import { Link, useParams } from 'react-router-dom';
import { FieldValues, SubmitHandler, useForm, FormProvider } from 'react-hook-form';
import { BookmarkSquareIcon } from '@heroicons/react/24/outline';
import * as Components from '../../components';
import { IconButton, InlineMessageBar, BarMessage } from '../../components';
import { useHttpRequest } from '../../hooks/useHttpRequest';
import * as AggregateType from '../../");
            this.Write(this.ToStringHelper.ToStringWithCulture(types.ImportName));
            this.Write("\'\r\nimport { ");
            this.Write(this.ToStringHelper.ToStringWithCulture(new FormOfAggregateInstance.Component(_instance).ComponentName));
            this.Write(@" } from './components'

export default function () {

  const [, dispatch] = useAppContext()
  
  const { get, post } = useHttpRequest()
  const { instanceKey } = useParams()
  const [instanceName, setInstanceName] = useState<string | undefined>('')
  const [fetched, setFetched] = useState(false)
  const defaultValues = useCallback(async () => {
    if (!instanceKey) return AggregateType.");
            this.Write(this.ToStringHelper.ToStringWithCulture(new types.AggregateInstanceInitializerFunction(_instance).FunctionName));
            this.Write("()\r\n    const encoded = window.encodeURI(instanceKey)\r\n    const response = await" +
                    " get(`");
            this.Write(this.ToStringHelper.ToStringWithCulture(GetFindCommandApi()));
            this.Write("/${encoded}`)\r\n    setFetched(true)\r\n    if (response.ok) {\r\n      const response" +
                    "Data = response.data as AggregateType.");
            this.Write(this.ToStringHelper.ToStringWithCulture(_instance.Item.TypeScriptTypeName));
            this.Write("\r\n      setInstanceName(responseData.");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstanceBase.INSTANCE_NAME));
            this.Write(")\r\n      return responseData\r\n    } else {\r\n      return AggregateType.");
            this.Write(this.ToStringHelper.ToStringWithCulture(new types.AggregateInstanceInitializerFunction(_instance).FunctionName));
            this.Write(@"()
    }
  }, [instanceKey])

  const reactHookFormMethods = useForm({ defaultValues })

  const [errorMessages, setErrorMessages] = useState<BarMessage[]>([])
  const onSave: SubmitHandler<FieldValues> = useCallback(async data => {
    const response = await post<AggregateType.");
            this.Write(this.ToStringHelper.ToStringWithCulture(_instance.Item.TypeScriptTypeName));
            this.Write(">(`");
            this.Write(this.ToStringHelper.ToStringWithCulture(GetUpdateCommandApi()));
            this.Write("`, data)\r\n    if (response.ok) {\r\n      setErrorMessages([])\r\n      dispatch({ ty" +
                    "pe: \'pushMsg\', msg: `${response.data.");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstanceBase.INSTANCE_NAME));
            this.Write(@"}を更新しました。` })
    } else {
      setErrorMessages([...errorMessages, ...response.errors])
    }
  }, [errorMessages, dispatch, post])

  if (!fetched) return <></>

  return (
    <FormProvider {...reactHookFormMethods}>
      <form className=""page-content-root"" onSubmit={reactHookFormMethods.handleSubmit(onSave)}>
        <h1 className=""text-base font-semibold select-none py-1"">
          <Link to=""");
            this.Write(this.ToStringHelper.ToStringWithCulture(GetMultiViewUrl()));
            this.Write("\">");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("</Link>\r\n          &nbsp;&#047;&nbsp;\r\n          <span className=\"select-all\">{in" +
                    "stanceName}</span>\r\n        </h1>\r\n        <div className=\"flex flex-col space-y" +
                    "-1 p-1 bg-neutral-200\">\r\n          <");
            this.Write(this.ToStringHelper.ToStringWithCulture(new FormOfAggregateInstance.Component(_instance).ComponentName));
            this.Write(" />\r\n        </div>\r\n        <InlineMessageBar value={errorMessages} onChange={se" +
                    "tErrorMessages} />\r\n        <IconButton fill icon={BookmarkSquareIcon} className" +
                    "=\"self-start\">更新</IconButton>\r\n      </form>\r\n    </FormProvider>\r\n  )\r\n}\r\n");
            return this.GenerationEnvironment.ToString();
        }
    }
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class SingleViewBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        public System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
