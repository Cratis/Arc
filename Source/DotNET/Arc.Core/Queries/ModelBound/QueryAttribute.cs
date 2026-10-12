// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Marks an eligible static method on a read model as a query.
/// </summary>
/// <remarks>
/// This marker selects endpoints only when the read model requires query attributes.
/// Otherwise conventional discovery is unchanged; the marked declaration is still validated.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class QueryAttribute : Attribute;
