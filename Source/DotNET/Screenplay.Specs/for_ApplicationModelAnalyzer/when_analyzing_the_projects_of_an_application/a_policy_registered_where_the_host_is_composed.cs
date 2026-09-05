// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing_the_projects_of_an_application;

/// <summary>
/// An artifact names a policy and the composition root says what it means, and the whole point of layering an
/// application is that the host is not where the behavior lives. Reading only the project the artifact is in would
/// find no registration at all and declare every policy of a layered application as one whose rule is not stated.
/// </summary>
public class a_policy_registered_where_the_host_is_composed : Specification
{
    const string Framework = """
        using System;

        namespace Microsoft.AspNetCore.Authorization;

        public class AuthorizationPolicyBuilder
        {
            public AuthorizationPolicyBuilder RequireRole(params string[] roles) => this;
        }

        public class AuthorizationOptions
        {
            public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
            {
            }
        }
        """;

    const string Composition = """
        using Microsoft.AspNetCore.Authorization;

        namespace Library.Host;

        public static class Composition
        {
            public static void Authorization(AuthorizationOptions options) =>
                options.AddPolicy("CanReserve", policy => policy.RequireRole("Librarian"));
        }
        """;

    const string Slice = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Lending.Reserving;

        [Command]
        [Authorize(Policy = "CanReserve")]
        public record ReserveBook(string Isbn)
        {
            public void Handle()
            {
            }
        }
        """;

    Compilation _domain;
    Compilation _host;
    ApplicationModelAnalysis _analysis;

    void Establish()
    {
        _domain = Analyzed.Project(
            "Library.Domain",
            [],
            ("Source/Library.Domain/Domain.cs", "namespace Library.Domain;"),
            ("Source/Library.Domain/Lending/Reserving/Reserving.cs", Slice));

        _host = Analyzed.Project(
            "Library.Host",
            [],
            ("Source/Library.Host/Authorization.cs", Framework),
            ("Source/Library.Host/Composition.cs", Composition));
    }

    void Because() => _analysis = Analyzed.Projects(_domain, _host);

    PolicyModel Policy => _analysis.Model.Policies.Single();

    [Fact] void should_compile_the_domain_project() => Analyzed.ErrorsIn(_domain).ShouldBeEmpty();
    [Fact] void should_compile_the_host_project() => Analyzed.ErrorsIn(_host).ShouldBeEmpty();
    [Fact] void should_declare_the_policy_the_command_names() => Policy.Name.ShouldEqual("CanReserve");
    [Fact] void should_state_what_the_host_registered_it_as_requiring() => Policy.Requirement.ShouldBeOfExactType<RoleRequirement>();
    [Fact] void should_not_report_it_as_unregistered() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable).ShouldBeFalse();
}
