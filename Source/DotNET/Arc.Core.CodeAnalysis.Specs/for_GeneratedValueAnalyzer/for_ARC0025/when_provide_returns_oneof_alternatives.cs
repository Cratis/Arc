// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_provide_returns_oneof_alternatives : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("OneOf.OneOf<CartId, int>")]
    [InlineData("Task<OneOf.OneOf<CartId, int>>")]
    [InlineData("OneOf.OneOf<(CartId Id, int Count), string>")]
    [InlineData("(int Count, OneOf.OneOf<CartId, string> Id)")]
    [InlineData("ProvidedId")]
    public async Task should_warn_for_a_generated_type_in_a_runtime_unwrapped_alternative(string returnType) => await VerifyDiagnostic(
        $@"
        namespace OneOf
        {{
            public interface IOneOf {{ }}
            public readonly struct OneOf<T0, T1> : IOneOf {{ }}
            public abstract class OneOfBase<T0, T1> : IOneOf {{ }}
        }}
        public class ProvidedId : OneOf.OneOfBase<CartId, string>;
        [Command]
        public record Allocate
        {{
            public {returnType} Provide() => default;
            public void Handle([{{|#0:GeneratedValue|}}] CartId value) {{ }}
        }}",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");
}
