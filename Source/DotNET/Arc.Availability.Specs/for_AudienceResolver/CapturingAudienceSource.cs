// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

class CapturingAudienceSource : IAudienceSource
{
    public AudienceSubject Subject { get; private set; }

    public ValueTask<IReadOnlyCollection<AudienceGrant>> GetGrants(AudienceSubject subject, CancellationToken cancellationToken = default)
    {
        Subject = subject;
        return new ValueTask<IReadOnlyCollection<AudienceGrant>>([]);
    }

    public IDisposable OnChanged(Func<AudienceChange, ValueTask> handler) => new Subscription();

    sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
