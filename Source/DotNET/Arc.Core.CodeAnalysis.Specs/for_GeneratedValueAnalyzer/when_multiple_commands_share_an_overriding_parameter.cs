// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer;

public class when_multiple_commands_share_an_overriding_parameter : given.a_generated_value_analyzer
{
    [Fact] async Task should_report_the_inherited_attribute_once_at_the_overriding_parameter() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public virtual void Handle([GeneratedValue] Guid original) { }
        }
        public record Intermediate : Parent
        {
            public override void Handle(Guid {|#0:value|}) { }
        }
        [Command] public record First : Intermediate;
        [Command] public record Second : Intermediate;",
        "ARC0022",
        "value",
        "Guid");
}
