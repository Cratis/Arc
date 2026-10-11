// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Testing.for_QueryScenario;

[ReadModel]
public record StreamRejectionReadModel(string Name)
{
    [AllowAnonymous]
    public static IAsyncEnumerable<StreamRejectionReadModel> AsyncStream(StreamInvocationTracker tracker)
    {
        // Count method invocation, even if the returned async iterator is never enumerated.
        tracker.RecordInvocation();
        return Enumerate();

        static async IAsyncEnumerable<StreamRejectionReadModel> Enumerate()
        {
            await Task.CompletedTask;
            yield return new("First");
            yield return new("Second");
        }
    }
}
