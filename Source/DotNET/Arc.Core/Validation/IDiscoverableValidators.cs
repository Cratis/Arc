// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Defines a service that can lookup <see cref="IValidator"/> for a given model type.
/// </summary>
public interface IDiscoverableValidators
{
    /// <summary>
    /// Try to get a validator for the given model type.
    /// </summary>
    /// <param name="modelType">Type of model to get a validator for.</param>
    /// <param name="validator">The <see cref="IValidator"/> if found.</param>
    /// <returns>True if a validator was found, false otherwise.</returns>
    bool TryGet(Type modelType, [MaybeNullWhen(false)] out IValidator validator);

    /// <summary>
    /// Try to get a validator for the given model type, resolving it from the supplied <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="modelType">Type of model to get a validator for.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve the validator and its dependencies from.</param>
    /// <param name="validator">The <see cref="IValidator"/> if found.</param>
    /// <returns>True if a validator was found, false otherwise.</returns>
    /// <remarks>
    /// Resolving from a command-scoped provider lets a validator take scoped dependencies — such as read models
    /// resolved for the command's event source id — exactly like a command's <c>Handle()</c> method.
    /// </remarks>
    bool TryGet(Type modelType, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IValidator validator);
}
