
namespace haldoc.AutoGenerated.Mvc {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    
    public class 営業所Controller : Controller {
        public 営業所Controller(haldoc.Core.ProjectContext context) {
            _projectContext = context;
            _aggregate = _projectContext.FindAggregate(typeof(haldoc.営業所));
        }
        private readonly haldoc.Core.Aggregate _aggregate;
        private readonly haldoc.Core.ProjectContext _projectContext;
        
        public IActionResult List() {
            var actionResult = _projectContext.MapToListView(_aggregate);
            if (actionResult == null) return NotFound();
            return View(actionResult.View, actionResult.Model);
        }
        public IActionResult ClearSearchCondition(haldoc.Runtime.ListViewModel<営業所__SearchCondition, 営業所__ListItem> model) {
            throw new NotImplementedException();
        }
        public IActionResult ExecuteSearch(haldoc.Runtime.ListViewModel<営業所__SearchCondition, 営業所__ListItem> model) {
            throw new NotImplementedException();
        }
        
        public IActionResult Create() {
            var actionResult = _projectContext.MapToCreateView(_aggregate);
            if (actionResult == null) return NotFound();
            return View(actionResult.View, actionResult.Model);
        }
        [HttpPost]
        public IActionResult SaveNewInstance(haldoc.Runtime.SingleViewModel<営業所> model) {
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