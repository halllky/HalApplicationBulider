using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest.Tests {
    partial class 観点 {

        [UseDataPatterns]
        public async Task Webから追加更新削除(DataPattern pattern) {

            if (pattern.Name != DataPattern.FILENAME_001) {
                Assert.Warn($"期待結果が定義されていません: {pattern.Name}");
                return;
            }

            // コードを再作成
            File.WriteAllText(TestProject.Current.SchemaXml.GetPath(), pattern.LoadXmlString());
            TestProject.Current.CodeGenerator.UpdateAutoGeneratedCode();

            // 開始
            using var launcher = TestProject.Current.CreateLauncher();
            var exceptions = new List<Exception>();
            launcher.OnError += (s, e) => {
                exceptions.Add(new Exception(e.ToString()));
                launcher.Terminate();
                Assert.Fail($"Launcher catched error: {e}");
            };

            launcher.Launch();
            launcher.WaitForReady();

            using var driver = TestProject.CreateWebDriver();
            await driver.InitializeData();

            // 準備: 参照先を作る
            driver.FindElement(Util.ByInnerText("参照先")).Click();
            await driver.AddNewItemAndNavigateToCreateView();

            driver.FindElement(By.Name("参照先集約ID")).SendKeys("あああああ");
            driver.FindElement(By.Name("参照先集約名")).SendKeys("いいいいい");
            driver.FindElement(Util.ByInnerText("一時保存")).Click();
            driver.CommitLocalRepositoryChanges();

            // 参照元を作成
            driver.FindElement(Util.ByInnerText("参照元")).Click();
            await driver.AddNewItemAndNavigateToCreateView();

            driver.FindElement(By.Name("参照元集約ID")).SendKeys("ううううう");
            driver.FindElement(By.Name("参照元集約名")).SendKeys("えええええ");
            driver.FindElement(By.Name("参照")).SendKeys("いいいいい");
            driver.FindElement(By.Name("参照")).SendKeys(Keys.Tab);
            driver.FindElement(Util.ByInnerText("一時保存")).Click();
            driver.CommitLocalRepositoryChanges();

            // 作成ができているか確認
            driver.FindElement(Util.ByInnerText("参照元")).Click();
            await Util.WaitUntil(() => driver.FindElements(Util.ByInnerText("ううううう")).Count > 0);
            Assert.Multiple(() => {
                Assert.That(driver.FindElements(Util.ByInnerText("ううううう")).Count, Is.EqualTo(1));
                Assert.That(driver.FindElements(Util.ByInnerText("えええええ")).Count, Is.EqualTo(1));
            });

            driver.FindElements(Util.ByInnerText("詳細"))[Util.DUMMY_DATA_COUNT].Click();

            // 参照元を更新

            // 更新ができているか確認

            // 参照元を削除

            // 削除ができているか確認

            if (exceptions.Count != 0) {
                throw new AggregateException(exceptions.ToArray());
            }

            TestContext.WriteLine("正常終了");
        }
    }
}
