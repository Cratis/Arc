// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_the_command_is_open_generic : given.a_generated_value_analyzer
{
    [Fact] async Task should_defer_a_type_parameter_check_until_the_type_is_closed() => await VerifyValid(@"
        [Command] public record Allocate<T>
        {
            public void Handle([GeneratedValue] T value) { }
        }");

    [Fact] async Task should_defer_an_inherited_type_parameter_check() => await VerifyValid(@"
        public record Parent<T>
        {
            public virtual void Handle([GeneratedValue] T original) { }
        }
        [Command] public record Allocate<T> : Parent<T>
        {
            public override void Handle(T value) { }
        }");

    [Fact] async Task should_still_report_an_invalid_concrete_parameter_type() => await VerifyDiagnostic(
        @"
        [Command] public record Allocate<T>
        {
            public void Handle([{|#0:GeneratedValue|}] Guid value) { }
        }",
        "ARC0022",
        "value",
        "Guid");
}
