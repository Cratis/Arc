// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_DeclaredAudiences;

public class when_created_by_the_existing_calls : Specification
{
    DeclaredAudiences _params;
    DeclaredAudiences _enumerable;
    DeclaredAudiences _none;
    IDeclaredAudiences _defaulted;

    void Because()
    {
        _params = new DeclaredAudiences("A", "B");
        _enumerable = new DeclaredAudiences(new List<AudienceName> { "A" });
        _none = new DeclaredAudiences();
        _defaulted = new DefaultDeclarations();
    }

    [Fact] void should_declare_from_params() => _params.IsDeclared("B").ShouldBeTrue();
    [Fact] void should_declare_from_an_enumerable() => _enumerable.All.Count.ShouldEqual(1);
    [Fact] void should_declare_nothing_without_arguments() => _none.All.Count.ShouldEqual(0);
    [Fact] void should_describe_names_without_description() => _params.Declarations.All(_ => _.Description.Length == 0 && _.Source.Length == 0).ShouldBeTrue();
    [Fact] void should_report_names_through_the_interface_for_older_implementations() => _defaulted.Declarations.Single().Name.Value.ShouldEqual("Z");

    class DefaultDeclarations : IDeclaredAudiences
    {
        public IReadOnlyCollection<AudienceName> All => [new AudienceName("Z")];

        public bool IsDeclared(AudienceName audience) => true;
    }
}
