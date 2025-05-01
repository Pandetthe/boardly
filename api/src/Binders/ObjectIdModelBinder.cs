using Microsoft.AspNetCore.Mvc.ModelBinding;
using MongoDB.Bson;

namespace Boardly.Api.Binders;

public class ObjectIdModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value) || !ObjectId.TryParse(value, out var objectId))
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid id format.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(objectId);
        return Task.CompletedTask;
    }
}