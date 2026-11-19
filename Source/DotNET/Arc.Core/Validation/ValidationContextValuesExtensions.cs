// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Contexts;
using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Extensions for reading the request-scoped context values from an <see cref="IValidationContext"/>.
/// </summary>
/// <remarks>
/// The values are the ones the model adds to <c>$context</c> with <c>provide</c>, resolved lazily, once, for the request that
/// is being validated. A validation outside Arc's command or query pipeline reads every value as absent.
/// </remarks>
public static class ValidationContextValuesExtensions
{
    /// <summary>
    /// The key under which the values are kept in the root context data of the validation.
    /// </summary>
    internal const string ContextKey = "Cratis.Arc.ContextValues";

    /// <summary>
    /// Gets the provided values with the asynchronous and typed reads of <see cref="ContextValues"/>.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext"/> to read from.</param>
    /// <returns>The <see cref="ContextValues"/>; <see cref="ContextValues.Empty"/> when the request provides none.</returns>
    public static ContextValues ProvidedValues(this IValidationContext context) =>
        context.RootContextData.TryGetValue(ContextKey, out var value) && value is ContextValues values ? values : ContextValues.Empty;

    /// <summary>
    /// Gets the provided values, readable by name such as <c>context.Values().Vessel</c>.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext"/> to read from.</param>
    /// <returns>The values. An absent value is <see langword="null"/>, which is unknown.</returns>
    [RequiresDynamicCode("Reading a value by name uses dynamic dispatch. Use ProvidedValues or ValuesAs where dynamic code is unavailable.")]
    public static dynamic Values(this IValidationContext context) => context.ProvidedValues().AsDynamic();

    /// <summary>
    /// Gets the provided values as a statically typed payload without dynamic dispatch.
    /// </summary>
    /// <typeparam name="T">A type whose public properties, or constructor parameters, are named after the provided values.</typeparam>
    /// <param name="context">The <see cref="IValidationContext"/> to read from.</param>
    /// <returns>The provided values as <typeparamref name="T"/>.</returns>
    /// <exception cref="ContextValuesTypeMismatch">The provided values do not fit <typeparamref name="T"/>.</exception>
    public static T ValuesAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IValidationContext context) => context.ProvidedValues().ValuesAs<T>();
}
