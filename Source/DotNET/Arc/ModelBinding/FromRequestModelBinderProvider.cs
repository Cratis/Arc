// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Cratis.Arc.ModelBinding;

/// <summary>
/// Represents a <see cref="IModelBinderProvider"/> supporting the <see cref="FromRequestBindingSource"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FromRequestModelBinderProvider"/> class.
/// </remarks>
/// <param name="bodyModelBinderProvider">The <see cref="BodyModelBinderProvider"/>.</param>
/// <param name="complexObjectModelBinderProvider">The <see cref="ComplexObjectModelBinderProvider"/>.</param>
public class FromRequestModelBinderProvider(
    BodyModelBinderProvider bodyModelBinderProvider,
    ComplexObjectModelBinderProvider complexObjectModelBinderProvider) : IModelBinderProvider
{
    readonly BodyModelBinderProvider _bodyModelBinderProvider = bodyModelBinderProvider;
    readonly ComplexObjectModelBinderProvider _complexObjectModelBinderProvider = complexObjectModelBinderProvider;

    /// <inheritdoc/>
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.BindingInfo.BindingSource switch
        {
            BindingSource bs when !bs.CanAcceptDataFrom(FromRequestBindingSource.FromRequest) => null,
            BindingSource bs when bs.CanAcceptDataFrom(FromRequestBindingSource.FromRequest) => new FromRequestModelBinder(
                _bodyModelBinderProvider.GetBinder(context)!, _complexObjectModelBinderProvider.GetBinder(context)!),
            null => null,
            _ => null
        };
}
