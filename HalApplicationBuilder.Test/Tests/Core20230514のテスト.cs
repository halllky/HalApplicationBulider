using HalApplicationBuilder.CodeRendering20230514;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Test.Tests {
    public class Core20230514のテスト {
        [Fact]
        public void Test1() {

            var xDocument = XDocument.Parse(RDRA_XML.Trim());

            var successToCreateBuidler = Core20230514.AppSchemaBuilder.FromXml(xDocument, out var builder, out var errors);
            var successToBuildSchema = builder.TryBuild(out var appSchema, out var errors1);

            var entries = appSchema.AllAggregates().ToArray();

            var codeRenderingContext = new CodeRenderingContext {
                Config = Core20230514.Config.FromXml(xDocument),
                Schema = appSchema,
            };
            var selectMethod = new CodeRendering20230514.EFCore.Search(codeRenderingContext).TransformText();

            Assert.True(successToCreateBuidler);
            Assert.True(successToBuildSchema);
            Assert.Equal(7, entries.Length);
        }

        private const string RDRA_XML = @"
<?xml version=""1.0"" encoding=""UTF-8"" ?>

<!--アプリケーション名を変更してください。-->
<YourApplicationName>
  <!-- システム価値 -->
  <要求>
    <ID type=""id"" key="""" />
    <詳細 type=""sentence"" />
    <根拠 type=""sentence"" />
  </要求>
  <アクター>
    <ID type=""id"" key="""" />
    <アクター名 type=""word"" key="""" name="""" />
  </アクター>

  <!-- システム外部境界 -->
  <ビジネスユースケース>
    <ID type=""id"" key="""" />
    <説明 type=""sentence"" name="""" />
    <アクター refTo=""/アクター"" />
  </ビジネスユースケース>

  <!-- システム境界 -->
  <画面>
    <ID type=""id"" key="""" />
    <画面名 type=""word"" key="""" name="""" />
  </画面>
  <システムユースケース>
    <ID type=""id"" key="""" />
    <説明 type=""sentence"" name="""" />
    <BUC refTo=""/ビジネスユースケース"" />
  </システムユースケース>
  <イベント>
    <ID type=""id"" key="""" />
    <イベント名称 type=""word"" key="""" name="""" />
  </イベント>

  <!-- システム -->
  <機能>
    <ID type=""id"" key="""" />
    <機能名 type=""word"" key="""" name="""" />
    <更新か参照か type=""word"" key="""" name="""" />
  </機能>

  <_Config>
    <Enum>
      <!--列挙体を定義してください。-->
      <機能種別>
        <参照系機能 />
        <更新系機能 />
      </機能種別>
    </Enum>

    <!--ソースコードの自動生成に関する設定: 出力先ディレクトリ-->
    <OutDirRoot>./dist</OutDirRoot>
    <OutDirRelativePath>
      <EFCore>EntityFramework/__AutoGenerated</EFCore>
      <MvcModel>Models/__AutoGenerated</MvcModel>
      <MvcView>Views/_AutoGenerated</MvcView>
      <MvcController>Controllers/__AutoGenerated</MvcController>
    </OutDirRelativePath>
    
    <!--ソースコードの自動生成に関する設定: 出力時名前空間-->
    <Namespace>
      <DbContextNamespace>HalApplicationBuilder.Test.DistMvc.EntityFramework</DbContextNamespace>
      <EntityNamespace>HalApplicationBuilder.Test.DistMvc.EntityFramework.Entities</EntityNamespace>
      <MvcModelNamespace>HalApplicationBuilder.Test.DistMvc.Models</MvcModelNamespace>
      <MvcControllerNamespace>HalApplicationBuilder.Test.DistMvc.Controllers</MvcControllerNamespace>
    </Namespace>
    
    <!--ソースコードの自動生成に関する設定: DbContextクラス名-->
    <DbContextName>MyDbContext</DbContextName>

  </_Config>
</YourApplicationName>
";
    }
}
