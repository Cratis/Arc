// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_an_unrelated_attribute_has_the_same_name : given.a_generated_value_analyzer
{
    [Fact] async Task should_ignore_the_unrelated_attribute() => await VerifyValid(@"
        public class GeneratedValueAttribute : Attribute;
        public record Example
        {
            public void Other([GeneratedValue] string value) { }
        }
        ");
}
