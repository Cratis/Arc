// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_cyclic_and_independent_bodies : Specification
{
    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([
        "export class First {\n    second!: Second;\n}",
        "export class Second {\n    first!: First;\n}",
        "export class Independent {}",
        "export class Dependent {\n    first!: First;\n}"
    ]);

    [Fact] void should_emit_independent_bodies_then_keep_unresolved_bodies_in_original_order() => _result.ShouldEqual(string.Join(Environment.NewLine,
        string.Empty,
        string.Empty,
        "export class Independent {}",
        string.Empty,
        "export class First {\n    second!: Second;\n}",
        string.Empty,
        "export class Second {\n    first!: First;\n}",
        string.Empty,
        "export class Dependent {\n    first!: First;\n}",
        string.Empty));
}
