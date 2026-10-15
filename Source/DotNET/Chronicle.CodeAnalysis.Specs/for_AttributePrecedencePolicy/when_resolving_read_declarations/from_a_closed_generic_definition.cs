// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_declarations;

public class from_a_closed_generic_definition : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _genericModel = true;
        _source = "public class Command { public void Handle(Model<Argument> model) { } } [Unprotected] public class Model<T>; public class Argument;";
    }
    void Because() => Resolve();
    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("True:False");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("True:False");
}
