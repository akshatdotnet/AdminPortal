using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UserManagement.Helpers;
using UserManagement.ViewModels;

namespace UserManagement.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequirePermissionAttribute : ActionFilterAttribute
    {
        private readonly string _permission;
        private readonly string? _controller;

        public RequirePermissionAttribute(string permission, string? controller = null)
        {
            _permission = permission;
            _controller = controller;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.Session
                .GetObjectFromJson<SessionUserViewModel>("CurrentUser");

            if (user == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // SuperAdmin always has access
            if (user.Roles.Contains("SuperAdmin")) return;

            var controllerName = _controller
                ?? context.RouteData.Values["controller"]?.ToString()
                ?? string.Empty;

            var perm = user.Permissions.FirstOrDefault(p =>
                p.ModuleName.Replace(" ", "")
                 .Equals(controllerName, StringComparison.OrdinalIgnoreCase));

            bool allowed = _permission.ToUpper() switch
            {
                "VIEW"   => perm?.CanView   ?? false,
                "CREATE" => perm?.CanCreate ?? false,
                "EDIT"   => perm?.CanEdit   ?? false,
                "DELETE" => perm?.CanDelete ?? false,
                _        => false
            };

            if (!allowed)
            {
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml"
                };
            }
        }
    }
}
