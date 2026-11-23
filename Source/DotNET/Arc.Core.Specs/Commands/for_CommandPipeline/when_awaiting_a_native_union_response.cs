// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Text.Json;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_awaiting_a_native_union_response : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Awaited(42));

    [Fact] void should_return_the_active_primitive() => _result.ResponseValue.ShouldEqual(42);
    [Fact] void should_serialize_the_other_active_case() => JsonSerializer.Serialize<Answer>(42, JsonSerializerOptions.Web).ShouldEqual("42");
}
#endif
