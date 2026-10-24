// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.Testing;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Testing;

namespace Cratis.Arc.Chronicle.for_DiscoveredConstraintsForScenario.when_providing;

public class twice_for_the_same_defaults : Specification
{
    Defaults _defaults;
    IImmutableList<IConstraintDefinition> _first;
    IImmutableList<IConstraintDefinition> _second;

    void Establish()
    {
        _defaults = new Defaults(Defaults.Instance.ClientArtifactsProvider);
        _first = new DiscoveredConstraintsForScenario(_defaults).Provide();
    }

    void Because() => _second = new DiscoveredConstraintsForScenario(_defaults).Provide();

    [Fact] void should_reuse_the_definitions() => _second.ShouldBeSame(_first);

    void Destroy() => ((IDisposable)_defaults.EventStore).Dispose();
}
