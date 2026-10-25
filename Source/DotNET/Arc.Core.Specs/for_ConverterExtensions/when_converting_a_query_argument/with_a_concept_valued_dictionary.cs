// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_query_argument;

public class with_a_concept_valued_dictionary
{
    [Fact]
    public void should_convert_each_value_to_its_concept()
    {
        var result = (IDictionary<string, Identity.IdentityName>)"""{"owner":"Alice"}"""
            .ConvertQueryArgument(typeof(IDictionary<string, Identity.IdentityName>), "parts", "Some.Query")!;
        result["owner"].ShouldEqual(new Identity.IdentityName("Alice"));
    }
}
