// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents a request to validate an object graph with <see cref="IModelGraphValidator"/>.
/// </summary>
/// <param name="Instance">The root of the object graph to validate.</param>
/// <param name="ServiceProvider">
/// The <see cref="IServiceProvider"/> to resolve validators and their dependencies from. When not supplied, the
/// ambient provider is used. Supply the request-scoped provider so a validator sees the same scope as the operation
/// it validates.
/// </param>
/// <param name="RootPath">
/// The member path that <paramref name="Instance"/> sits at, used to prefix every failure member. Empty for a command,
/// whose properties are already named from the client's perspective; the parameter name for a query argument, so a
/// failure reported by a nested validator is attributable to the argument that carried it.
/// </param>
public record ModelGraphValidationRequest(
    object Instance,
    IServiceProvider? ServiceProvider = default,
    string RootPath = "");
