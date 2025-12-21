using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DrinkShop.WebApi.DTO.ApiResponse; 
namespace DrinkShop.WebApi.Filters 
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Error = x.Value!.Errors.First().ErrorMessage
                    }).ToList();

                var response = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Data = errors
                };

                context.Result = new BadRequestObjectResult(response);
            }
        }
    }
}