// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_value_awaiting_a_native_union_response : given.a_native_union_pipeline
{
    async Task Because() => await Run(new ValueAwaited(_receipt));

    [Fact] void should_return_the_active_case() => _result.ResponseValue.ShouldEqual(_receipt);
}
#endif
