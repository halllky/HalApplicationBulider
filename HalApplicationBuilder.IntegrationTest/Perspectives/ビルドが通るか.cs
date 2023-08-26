using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest.Perspectives {
    partial class Perspective {
        [UseDataPatterns]
        public async Task ビルドが通るか(DataPattern pattern) {
            try {
                File.WriteAllText(SharedResource.Project.GetAggregateSchemaPath(), pattern.LoadXmlString());
                SharedResource.Project.UpdateAutoGeneratedCode();
                await SharedResource.Project.BuildAsync();
            } catch {
                NUnit.Framework.TestContext.Out.WriteLine("--- SCHEMA ---");
                NUnit.Framework.TestContext.Out.WriteLine(SharedResource.Project.Inspect().Graph.ToMermaidText());
                throw;
            }
        }
    }
}
