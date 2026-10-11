// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_the_attributed_enumerable_marker_has_a_counterfeit_assembly_identity : given.an_attributed_command_response
{
    void Because() => GenerateProxy(nameof(given.command_methods.ReturnsCollectionClaimedByCounterfeitAttributeMarker));

    [Fact] void should_preserve_the_collection_response() => _result.HasResponse.ShouldBeTrue();
}
