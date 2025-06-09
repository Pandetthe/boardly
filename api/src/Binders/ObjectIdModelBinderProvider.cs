using Microsoft.AspNetCore.Mvc.ModelBinding;
using MongoDB.Bson;

namespace Boardly.Api.Binders;

public class ObjectIdModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(ObjectId))
            return new ObjectIdModelBinder();
        return null;
    }
}
