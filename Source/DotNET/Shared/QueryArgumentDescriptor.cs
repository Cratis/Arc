// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Describes an argument a caller supplies to a query, as opposed to a dependency injected into it.
/// </summary>
/// <param name="Name">The argument name.</param>
/// <param name="Type">The argument type.</param>
/// <remarks>
/// Shared at the source level between the framework and the proxy generator, alongside
/// <see cref="QueryArgumentsModelConvention"/>, so both express a query's arguments the same way.
/// </remarks>
readonly record struct QueryArgumentDescriptor(string Name, Type Type);
