using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PMS.Web.Filters;

/// <summary>
/// Automatically returns 400 + validation errors when ModelState is invalid.
/// Apply to controllers or individual actions that need server-side MVC validation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors
                               .Select(e => e.ErrorMessage)
                               .ToArray());

            var isAjax = context.HttpContext.Request
                .Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    validationErrors = errors
                })
                { StatusCode = StatusCodes.Status422UnprocessableEntity };
            }
            else
            {
                context.Result = new BadRequestObjectResult(errors);
            }
        }
    }
}