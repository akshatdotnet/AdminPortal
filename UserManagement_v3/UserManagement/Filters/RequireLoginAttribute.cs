using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UserManagement.Helpers;
using UserManagement.ViewModels;

namespace UserManagement.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.Session
                .GetObjectFromJson<SessionUserViewModel>("CurrentUser");

            if (user == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new
                {
                    returnUrl = context.HttpContext.Request.Path
                });
            }
        }
    }
}
