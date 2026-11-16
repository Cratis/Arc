// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Testing.for_QueryScenario;

[ReadModel]
public record TaskStreamingReadModel
{
    [AllowAnonymous]
    public static Task<IAsyncEnumerable<TaskStreamingReadModel>> Stream() =>
        throw new InvalidOperationException("A snapshot must reject the stream before invoking the query.");
}
