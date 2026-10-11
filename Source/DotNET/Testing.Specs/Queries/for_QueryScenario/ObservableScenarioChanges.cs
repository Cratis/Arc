// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Cratis.Arc.Testing.for_QueryScenario;

public sealed class ObservableScenarioChanges
{
    public Channel<ObservableScenarioReadModel> Values { get; } = Channel.CreateUnbounded<ObservableScenarioReadModel>();
    public bool EnumerationStopped { get; private set; }

    public async IAsyncEnumerable<ObservableScenarioReadModel> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var value in Values.Reader.ReadAllAsync(cancellationToken))
            {
                yield return value;
            }
        }
        finally
        {
            EnumerationStopped = true;
        }
    }
}
