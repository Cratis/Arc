// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_declarations;

public class without_inheriting_from_an_interface : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _source = "public class Command : IHandler { public void Handle(Model model) { } } public interface IHandler { [Unprotected] void Handle([Unprotected] Model model); } public class Model;";
    }
    void Because() => Resolve();
    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("False:False");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("False:False");
}
