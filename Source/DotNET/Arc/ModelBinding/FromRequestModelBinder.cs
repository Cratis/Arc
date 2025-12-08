// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.ModelBinding;

/// <summary>
/// Represents a <see cref="IModelBinder"/> for <see cref="FromRequestBindingSource"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FromRequestModelBinder"/> class.
/// </remarks>
/// <param name="bodyModelBinder">The <see cref="IModelBinder"/> for resolving values from the HTTP body.</param>
/// <param name="complexModelBinder">The <see cref="IModelBinder"/> for resolving values from other parts of the HTTP request.</param>
public class FromRequestModelBinder(IModelBinder bodyModelBinder, IModelBinder complexModelBinder) : IModelBinder
{
    static readonly Dictionary<Type, MethodInfo> _isDefaultMethodsByType = [];

    readonly IModelBinder _bodyModelBinder = bodyModelBinder;
    readonly IModelBinder _complexModelBinder = complexModelBinder;

    /// <inheritdoc/>
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        await _bodyModelBinder.BindModelAsync(bindingContext);

        if (bindingContext.Result.IsModelSet)
        {
            var model = CloneObject(bindingContext.Result.Model!);
            await _complexModelBinder.BindModelAsync(bindingContext);
            var complexModel = CloneObject(bindingContext.Result.Model!);

            foreach (var property in model.GetType().GetProperties())
            {
                var modelValue = property.GetValue(model)!;
                var complexModelValue = property.GetValue(complexModel)!;
                var isModelDefault = IsDefaultValue(property.PropertyType, modelValue);
                var isComplexDefault = IsDefaultValue(property.PropertyType, complexModelValue);
                if (isModelDefault && !isComplexDefault)
                {
                    property.SetValue(model, complexModelValue);
                }
            }

            bindingContext.Result = ModelBindingResult.Success(model);
        }
        else
        {
            await _complexModelBinder.BindModelAsync(bindingContext);
        }
    }

    object CloneObject(object source)
    {
        var sourceAsString = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize(sourceAsString, source.GetType())!;
    }

    bool IsDefaultValue(Type type, object value)
    {
        if (!_isDefaultMethodsByType.TryGetValue(type, out var isDefaultMethod))
        {
            var equalityComparerType = typeof(DefaultValueChecker<>).MakeGenericType(type);
            isDefaultMethod = equalityComparerType.GetMethod(nameof(DefaultValueChecker<object>.IsDefault))!;
            _isDefaultMethodsByType[type] = isDefaultMethod;
        }

        return (bool)isDefaultMethod.Invoke(null, [value])!;
    }

    static class DefaultValueChecker<T>
    {
        public static bool IsDefault(T value) => EqualityComparer<T>.Default.Equals(value, default);
    }
}
