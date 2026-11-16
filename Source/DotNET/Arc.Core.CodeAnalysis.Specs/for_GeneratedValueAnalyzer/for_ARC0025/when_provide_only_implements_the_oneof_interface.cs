// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_provide_only_implements_the_oneof_interface : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public class ProvidedId<T> : OneOf.IOneOf;")]
    [InlineData("public class BaseId<T> : OneOf.IOneOf; public class ProvidedId<T> : BaseId<T>;")]
    public async Task should_use_the_generic_arguments_as_alternatives(string declaration) => await VerifyDiagnostic(
        $@"
        namespace OneOf {{ public interface IOneOf {{ }} }}
        {declaration}
        [Command] public record Allocate
        {{
            public ProvidedId<CartId> Provide() => new();
            public void Handle([{{|#0:GeneratedValue|}}] CartId value) {{ }}
        }}",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");
}
