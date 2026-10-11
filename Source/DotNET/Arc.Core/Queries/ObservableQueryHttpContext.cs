// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Carries the query and request state used to create an observable HTTP response.
/// </summary>
/// <param name="Query">The query being performed.</param>
/// <param name="Options">The snapshot wait options.</param>
/// <param name="Request">The request for interception and guards, if supplied.</param>
/// <param name="CancellationToken">The request cancellation token.</param>
internal sealed record ObservableQueryHttpContext(
    QueryContext Query,
    ObservableQueryHttpOptions Options,
    IHttpRequestContext? Request,
    CancellationToken CancellationToken);
