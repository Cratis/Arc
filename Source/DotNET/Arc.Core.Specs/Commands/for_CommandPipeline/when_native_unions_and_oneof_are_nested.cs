// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_native_unions_and_oneof_are_nested : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Wrapped(new Nested(OneOf<Answer, string>.FromT0(new Answer(_receipt)))));

    [Fact] void should_unwrap_every_container() => _result.ResponseValue.ShouldEqual(_receipt);
}
#endif
