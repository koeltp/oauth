using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OAuth.Server.Filters;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public ValidateModelAttribute()
    {
        Order = -3000;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrEmpty(m));

            var message = string.Join("；", errors);
            context.Result = new BadRequestObjectResult(new { message });
        }
    }
}

public static class ModelStateExtensions
{
    public static IActionResult Validate(this ControllerBase controller)
    {
        if (!controller.ModelState.IsValid)
        {
            var errors = controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrEmpty(m));

            var message = string.Join("；", errors);
            return controller.BadRequest(new { message });
        }
        return null;
    }
}