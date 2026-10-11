// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_an_enumerable_only_collection_is_returned : given.an_attributed_command_response
{
    void Because() => GenerateProxy(nameof(given.command_methods.ReturnsEnumerableOnlyCollection));

    [Fact] void should_have_no_response() => _result.HasResponse.ShouldBeFalse();
}
