// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_DeclaredAudiences;

public class when_created_from_declarations : Specification
{
    DeclaredAudiences _audiences;

    void Because() => _audiences = new DeclaredAudiences(
    [
        new AudienceDeclaration("Admin", "Runs the place", "Onboarding"),
        new AudienceDeclaration("Beta")
    ]);

    [Fact] void should_declare_the_names() => _audiences.All.Select(_ => _.Value).ShouldContainOnly("Admin", "Beta");
    [Fact] void should_keep_the_description() => _audiences.Declarations.First(_ => _.Name.Value == "Admin").Description.ShouldEqual("Runs the place");
    [Fact] void should_keep_the_source() => _audiences.Declarations.First(_ => _.Name.Value == "Admin").Source.ShouldEqual("Onboarding");
    [Fact] void should_still_answer_is_declared() => _audiences.IsDeclared("Beta").ShouldBeTrue();
}
