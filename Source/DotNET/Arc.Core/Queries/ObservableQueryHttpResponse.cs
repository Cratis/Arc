// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the HTTP response for an observable query.
/// </summary>
/// <param name="Result">The <see cref="QueryResult"/> to send.</param>
/// <param name="StatusCode">The HTTP <see cref="HttpStatusCode"/> to send.</param>
public readonly record struct ObservableQueryHttpResponse(QueryResult Result, HttpStatusCode StatusCode);
