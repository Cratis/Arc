// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_declarations;

public class without_inheriting_from_a_type_argument : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _genericModel = true;
        _source = "public class Command { public void Handle(Model<Argument> model) { } } public class Model<T>; [Unprotected] public class Argument;";
    }
    void Because() => Resolve();
    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("False:False");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("False:False");
}
