// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.Testing;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Testing;

namespace Cratis.Arc.Chronicle.for_DiscoveredConstraintsForScenario.when_providing;

public class for_different_defaults : Specification
{
    Defaults _defaults;
    Defaults _otherDefaults;
    IImmutableList<IConstraintDefinition> _first;
    IImmutableList<IConstraintDefinition> _second;

    void Establish()
    {
        _defaults = new Defaults(Defaults.Instance.ClientArtifactsProvider);
        _otherDefaults = new Defaults(Defaults.Instance.ClientArtifactsProvider);
        _first = new DiscoveredConstraintsForScenario(_defaults).Provide();
    }

    void Because() => _second = new DiscoveredConstraintsForScenario(_otherDefaults).Provide();

    [Fact] void should_not_share_the_definitions() => _second.ShouldNotBeSame(_first);

    void Destroy()
    {
        ((IDisposable)_defaults.EventStore).Dispose();
        ((IDisposable)_otherDefaults.EventStore).Dispose();
    }
}
