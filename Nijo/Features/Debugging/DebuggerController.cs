using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.Debugging {
    internal class DebuggerController {

        internal static SourceFile Render(CodeRenderingContext ctx) => new SourceFile {
            FileName = $"WebDebugger.cs",
            RenderContent = context => {

                return $$$""""
                    using Microsoft.AspNetCore.Mvc;
                    using System.Text.Json;
                    using Microsoft.EntityFrameworkCore;

                    namespace {{{ctx.Config.RootNamespace}}};

                    #if DEBUG
                    [ApiController]
                    [Route("[controller]")]
                    public class WebDebuggerController : ControllerBase {
                        public WebDebuggerController(IServiceProvider provider, AutoGeneratedApplicationService appsrv) {
                            _provider = provider;
                            _appSrv = appsrv;
                        }
                        private readonly IServiceProvider _provider;
                        private readonly AutoGeneratedApplicationService _appSrv;

                        [HttpPost("recreate-database")]
                        public HttpResponseMessage RecreateDatabase([FromQuery] bool generateDummyData = true, [FromQuery] int dummyDataCount = 4) {

                            _appSrv.{{{ApplicationService.BEFORE_DB_RECREATE}}}();

                            var dbContext = _provider.GetRequiredService<{{{ctx.Config.DbContextNamespace}}}.{{{ctx.Config.DbContextName}}}>();
                            dbContext.Database.EnsureDeleted();
                            dbContext.Database.EnsureCreated();

                            // ダミーデータ作成
                            if (generateDummyData) {
                                _appSrv.{{{Models.WriteModel2Features.DummyDataGenerator.APPSRV_METHOD_NAME}}}(dummyDataCount);
                            }

                            _appSrv.{{{ApplicationService.AFTER_DB_RECREATE}}}();

                            return new HttpResponseMessage {
                                StatusCode = System.Net.HttpStatusCode.OK,
                                Content = new StringContent("DBを再作成しました。"),
                            };
                        }

                        [HttpGet("secret-settings")]
                        public IActionResult GetSecretSettings() {
                            var runtimeSetting = _provider.GetRequiredService<{{{RuntimeSettings.ServerSetiingTypeFullName}}}>();
                            return this.JsonContent(runtimeSetting);
                        }
                    }
                    #endif
                    """";
            },
        };
    }
}
