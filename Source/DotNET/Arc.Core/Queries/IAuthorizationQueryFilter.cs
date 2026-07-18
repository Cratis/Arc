// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines an <see cref="IQueryFilter"/> that authorizes query execution and must run before other filters.
/// </summary>
/// <remarks>
/// Filters implementing this interface are evaluated before ordinary query filters, independent of the order in
/// which they are discovered. This guarantees a forbidden caller is denied (403) before any validation runs — the
/// filter chain short-circuits on the authorization verdict, so validation never executes and no validation detail
/// is returned to a caller with no rights to the endpoint.
/// </remarks>
public interface IAuthorizationQueryFilter : IQueryFilter;
