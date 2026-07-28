// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Roles are declared in two shapes - the constructor form and the named argument form - and reading only one of them
/// silently drops half the authorization in an application. Both are read, which is the bug in Arc's own proxy
/// generator that this deliberately does not repeat.
/// </summary>
public class a_command_authorized_in_both_shapes : Specification
{
    const string Source = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Inventory.Adding;

        [Command]
        [Roles("Librarian")]
        public record AddBookByRolesAttribute(string Title)
        {
            public void Handle()
            {
            }
        }

        [Command]
        [Authorize(Roles = "Archivist")]
        public record AddBookByNamedArgument(string Title)
        {
            public void Handle()
            {
            }
        }

        [Command]
        [Authorize]
        public record AddBookByAuthenticationAlone(string Title)
        {
            public void Handle()
            {
            }
        }

        [Command]
        public record AddBookAnonymously(string Title)
        {
            public void Handle()
            {
            }
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    CommandModel Command(string name) => _analysis.Slice().Commands.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_read_the_constructor_form() => Command("AddBookByRolesAttribute").Authorization!.Roles.ShouldContainOnly(["Librarian"]);
    [Fact] void should_read_the_named_argument_form() => Command("AddBookByNamedArgument").Authorization!.Roles.ShouldContainOnly(["Archivist"]);
    [Fact] void should_require_an_authenticated_caller_when_no_role_is_named() => Command("AddBookByAuthenticationAlone").Authorization!.RequiresAuthentication.ShouldBeTrue();
    [Fact] void should_name_no_role_when_none_was_declared() => Command("AddBookByAuthenticationAlone").Authorization!.Roles.ShouldBeEmpty();
    [Fact] void should_require_nothing_of_a_command_that_declares_nothing() => Command("AddBookAnonymously").Authorization.ShouldBeNull();
    [Fact] void should_declare_a_policy_for_every_role() => _analysis.Model.Policies.Select(_ => _.Name).ShouldContainOnly(["Archivist", "Librarian"]);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
