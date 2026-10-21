// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_declarations;

public class with_an_unprotected_method : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _source = "[ReadProtection(ReadProtectionMode.On)] public class Command { [Unprotected] public void Handle(Model model) { } } public class Model;";
    }
    void Because() => Resolve();
    [Fact] void should_record_the_body_without_opting_out_the_parameter_by_reflection() => _runtime.ShouldEqual("False:True");
    [Fact] void should_record_the_body_without_opting_out_the_parameter_by_roslyn() => _roslyn.ShouldEqual("False:True");
}
