// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_the_type_is_not_generatable : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("Guid")]
    [InlineData("string")]
    [InlineData("HasNew")]
    [InlineData("WrongSelf")]
    [InlineData("Impostor")]
    public async Task should_report_the_invalid_type(string type) => await VerifyDiagnostic(
        $@"
        public record HasNew {{ public static HasNew New() => new(); }}
        public record WrongSelf : IGeneratable<CartId> {{ public static CartId New() => CartId.New(); }}
        namespace Foreign {{ public interface IGeneratable<T> {{ }} }}
        public record Impostor : Foreign.IGeneratable<Impostor>;
        [Command]
        public record Allocate
        {{
            public void Handle([{{|#0:GeneratedValue|}}] {type} value) {{ }}
        }}",
        "ARC0022",
        "value",
        type == "string" ? "String" : type);
}
