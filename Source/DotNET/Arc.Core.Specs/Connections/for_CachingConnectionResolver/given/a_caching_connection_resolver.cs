// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CachingConnectionResolver.given;

public class a_caching_connection_resolver : Specification
{
    protected IConnectionResolver _inner;
    protected ManualTimeProvider _time;
    protected ConnectionRequest _request;
    protected ResolvedConnection _resolved;
    protected CachingConnectionResolver _resolver;
    protected TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    void Establish()
    {
        _time = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
        _request = new(WellKnownConnectionStorage.SqlServer, ConnectionPurpose.Runtime, new Dictionary<string, string> { ["tenant"] = "acme" });
        _resolved = new(Database: "tenant-acme");
        _inner = Substitute.For<IConnectionResolver>();
        _inner.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>()).Returns(_ => _resolved);
        _resolver = new(_inner, _cacheDuration, _time);
    }

    protected sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
