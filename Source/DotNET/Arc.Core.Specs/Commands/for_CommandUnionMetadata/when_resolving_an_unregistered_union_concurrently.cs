// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandUnionMetadata;

public class when_resolving_an_unregistered_union_concurrently : Specification
{
    CommandUnionMetadataRegistry _registry;
    CommandUnionMetadata.Declaration _afterFailure;
    int _registrations;
    bool _failRegistration;
    readonly CommandUnionMetadata.Declaration[] _results = new CommandUnionMetadata.Declaration[32];

    void Establish() => _registry = new(() =>
    {
        Interlocked.Increment(ref _registrations);
        if (_failRegistration) throw new InvalidOperationException("Unrelated module initialization failed.");
    });
    void Because()
    {
        Parallel.For(0, _results.Length, index => _results[index] = _registry.For(typeof(Unregistered)));
        _failRegistration = true;
        _afterFailure = _registry.For(typeof(Unregistered));
    }

    [Fact] void should_activate_pending_modules_only_once() => _registrations.ShouldEqual(1);
    [Fact] void should_cache_the_absence_for_every_caller() => _results.Any(result => result is not null).ShouldBeFalse();
    [Fact] void should_not_reach_subsequent_unrelated_registration_failures() => _afterFailure.ShouldBeNull();

    record Unregistered;
}
