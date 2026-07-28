// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A constraint can name any type at all, and a type nothing declares as an event is a name no import can introduce -
/// importing it would state that a package declares an event it does not. This is what is left for the report to say.
/// </summary>
public class an_event_nothing_declares : Specification
{
    const string Contracts = """
        namespace Partners.Contracts;

        public record CustomerRegistered(string Name);
        """;

    const string Slice = """
        using Cratis.Chronicle.Events.Constraints;
        using Partners.Contracts;

        namespace Library.Customers.Registration;

        public class UniqueCustomerConstraint : IConstraint
        {
            public void Define(IConstraintBuilder builder) => builder.Unique<CustomerRegistered>();
        }
        """;

    static readonly (string Path, string Text)[] _sources = [("Library/Customers/Registration/Registration.cs", Slice)];

    MetadataReference _package;
    ApplicationModelAnalysis _analysis;
    ScreenplayDiagnostic _reported;

    void Establish() => _package = Analyzed.Package("Partners.Contracts", Contracts);

    void Because()
    {
        _analysis = Analyzed.SourceReferencing(_package, _sources);
        _reported = _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation);
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_package, _sources).ShouldBeEmpty();
    [Fact] void should_import_nothing() => _analysis.Model.Imports.ShouldBeEmpty();
    [Fact] void should_name_the_event_in_the_report() => _reported.Message.Contains("CustomerRegistered", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_locate_the_report_at_the_slice() => _reported.Location.ShouldEqual("Library.Customers.Registration");
    [Fact] void should_report_it_as_a_loss() => _reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
}
