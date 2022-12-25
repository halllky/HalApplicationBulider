
namespace haldoc.AutoGenerated.Mvc {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    
    public class 請求情報Controller : Controller {
        public 請求情報Controller(haldoc.Core.ProjectContext context) {
            _projectContext = context;
            _aggregate = _projectContext.FindAggregate(typeof(haldoc.請求情報));
        }
        private readonly haldoc.Core.Aggregate _aggregate;
        private readonly haldoc.Core.ProjectContext _projectContext;
        
        public IActionResult List() {
            var actionResult = _projectContext.MapToListView(_aggregate);
            if (actionResult == null) return NotFound();
            return View(actionResult.View, actionResult.Model);
        }
        public IActionResult ClearSearchCondition(haldoc.Runtime.ListViewModel<請求情報__SearchCondition, 請求情報__ListItem> model) {
            throw new NotImplementedException();
        }
        public IActionResult ExecuteSearch(haldoc.Runtime.ListViewModel<請求情報__SearchCondition, 請求情報__ListItem> model) {
            throw new NotImplementedException();
        }
        
        public IActionResult Create() {
            var actionResult = _projectContext.MapToCreateView(_aggregate);
            if (actionResult == null) return NotFound();
            return View(actionResult.View, actionResult.Model);
        }
        [HttpPost]
        public IActionResult SaveNewInstance(haldoc.Runtime.SingleViewModel<請求情報> model) {
            var actionResult = _projectContext.SaveNewInstance(_aggregate, model);
            if (actionResult.Errors.Any()) {
                foreach (var error in actionResult.Errors.SelectMany(e => e.MemberNames, (e, Member) => new { Member, e.ErrorMessage })) {
                    ModelState.AddModelError(error.Member, error.ErrorMessage);
                }
            }
            return View(actionResult.View, actionResult.Model);
        }
        
        public IActionResult Single() {
            throw new NotImplementedException();
        }
    }
}