// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class a_null_concept : given.date_inputs
{
    NullableConceptInput _result;
    string _json;

    void Because()
    {
        _result = JsonSerializer.Deserialize<NullableConceptInput>("""{"from":null}""", Options)!;
        _json = JsonSerializer.Serialize(_result, Options);
    }

    [Fact] void should_accept_null() => _result.From.ShouldBeNull();
    [Fact] void should_keep_the_default_null_omission() => _json.ShouldEqual("{}");
}
