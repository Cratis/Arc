// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0024;

public class when_handle_overrides_generated_names : given.a_generated_value_analyzer
{
    [Fact] async Task should_use_redeclared_names_instead_of_base_names() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public virtual void Handle([GeneratedValue(""base-first"")] CartId originalFirst, [GeneratedValue(""base-second"")] CartId originalSecond) { }
        }
        [Command] public record Allocate : Parent
        {
            public override void Handle([GeneratedValue(""shared"")] CartId first, [GeneratedValue(""shared"")] CartId {|#0:second|}) { }
        }",
        "ARC0024",
        "second",
        "Allocate");

    [Fact] async Task should_use_the_nearest_attribute_in_the_override_chain() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public virtual void Handle([GeneratedValue(""base-first"")] CartId originalFirst, [GeneratedValue(""base-second"")] CartId originalSecond) { }
        }
        public record Intermediate : Parent
        {
            public override void Handle([GeneratedValue(""shared"")] CartId middleFirst, [GeneratedValue(""shared"")] CartId middleSecond) { }
        }
        [Command] public record Allocate : Intermediate
        {
            public override void Handle(CartId first, CartId {|#0:second|}) { }
        }",
        "ARC0024",
        "second",
        "Allocate");

    [Fact] async Task should_use_the_overriding_parameter_name_for_an_inherited_unnamed_attribute() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public virtual void Handle([GeneratedValue] CartId originalFirst, [GeneratedValue(""first"")] CartId originalSecond) { }
        }
        [Command] public record Allocate : Parent
        {
            public override void Handle(CartId first, CartId {|#0:second|}) { }
        }",
        "ARC0024",
        "second",
        "Allocate");

    [Fact] async Task should_ignore_base_name_collisions_replaced_by_distinct_names() => await VerifyValid(@"
        public record Parent
        {
            public virtual void Handle([GeneratedValue(""shared"")] CartId originalFirst, [GeneratedValue(""shared"")] CartId originalSecond) { }
        }
        [Command] public record Allocate : Parent
        {
            public override void Handle([GeneratedValue(""first"")] CartId first, [GeneratedValue(""second"")] CartId second) { }
        }");
}
