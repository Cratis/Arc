// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_external_targets_and_releases : Specification
{
    const string Contracts = """
        using Cratis.Chronicle.Events;
        namespace Partners.Contracts;
        [EventType]
        public record UserRenamed(string Name);
        [EventType]
        public record UserDeleted();
        """;

    const string Source = """
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Events.Constraints;
        using Partners.Contracts;
        namespace Library.Users.Registration;
        [EventType]
        public record UserRegistered(string Name);
        public class UserNames : IConstraint
        {
            public void Define(IConstraintBuilder builder) => builder.Unique(unique => unique
                .WithName("UniqueUserNames")
                .On<UserRegistered>(e => e.Name)
                .On<UserRenamed>(e => e.Name)
                .RemovedWith<UserDeleted>());
        }
        """;

    /// <summary>
    /// The pre-existing primary-external-target case, independent of additional rules and releases.
    /// </summary>
    const string Baseline = """
        using Cratis.Chronicle.Events.Constraints;
        using Partners.Contracts;
        namespace Library.Users.Registration;
        public class UserNames : IConstraint
        {
            public void Define(IConstraintBuilder builder) => builder.Unique(unique => unique
                .WithName("UniqueUserNames")
                .On<UserRenamed>(e => e.Name));
        }
        """;

    const string DomainMetadata = "A domain declaration is authoring metadata and is not part of ESM v1 behavior.";
    const string PrimaryImportLimit = "Import 'Partners.Contracts.UserRenamed' is not supported by ESM v1 binding.";
    const string ReleaseImportLimit = "Import 'Partners.Contracts.UserDeleted' is not supported by ESM v1 binding.";
    const string PrimaryConstraintLimit = "Constraint 'UniqueUserNames' names event 'UserRenamed', which the application does not declare.";

    Compilation _compilation;
    Compilation _baselineCompilation;
    ScreenplayGenerationResult _result;
    ScreenplayGenerationResult _baselineResult;
    ScreenplayVerification _verification;
    ScreenplayVerification _baselineVerification;

    void Establish()
    {
        var package = Analyzed.Package("Partners.Contracts", Contracts);
        _compilation = Analyzed.Compile([package], ("Library/Users/Registration/Users.cs", Source));
        _baselineCompilation = Analyzed.Compile([package], ("Library/Users/Registration/Users.cs", Baseline));
    }

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(_compilation, new ScreenplayOptions());
        _verification = new ScreenplayVerifier().Verify(_result.Source);
        _baselineResult = new ScreenplayGenerator().Generate(_baselineCompilation, new ScreenplayOptions());
        _baselineVerification = new ScreenplayVerifier().Verify(_baselineResult.Source);
    }

    [Fact] void should_compile_the_application_source() => Analyzed.ErrorsIn(_compilation).ShouldBeEmpty();
    [Fact] void should_declare_the_primary_event() => _result.Source.ShouldContain("event UserRegistered");
    [Fact] void should_import_the_additional_target() => _result.Source.ShouldContain("import Partners.Contracts.UserRenamed");
    [Fact] void should_import_the_release_event() => _result.Source.ShouldContain("import Partners.Contracts.UserDeleted");
    [Fact] void should_preserve_the_additional_rule() => _result.Source.ShouldContain("unique name on UserRenamed");
    [Fact] void should_preserve_the_release() => _result.Source.ShouldContain("released by UserDeleted");
    [Fact] void should_compile_the_document() => _verification.Compiles.ShouldBeTrue();
    [Fact] void should_preserve_exactly_the_baseline_binding_limits_and_the_release_import_limit() => _verification.BindingDiagnostics.Select(_ => (_.Code, _.Message)).ShouldContainOnly([("PLAY0270", DomainMetadata), ("PLAY0268", PrimaryImportLimit), ("PLAY0390", PrimaryConstraintLimit), ("PLAY0268", ReleaseImportLimit)]);
    [Fact] void should_report_the_same_standalone_code_and_severity_for_each_binding_limit() => _result.Diagnostics.Select(_ => (_.Code, _.Severity)).ShouldContainOnly([("SP0056", ScreenplayDiagnosticSeverity.Warning), ("SP0056", ScreenplayDiagnosticSeverity.Warning), ("SP0056", ScreenplayDiagnosticSeverity.Warning)]);
    [Fact] void should_preserve_the_binding_reasons_in_the_generator_diagnostics() => _verification.BindingDiagnostics.Where(_ => _.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).All(binding => _result.Diagnostics.Any(report => report.Message.Contains(binding.Message, StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_round_trip_stably() => Verification.RoundTrip.For(_verification.Application!).IsStable.ShouldBeTrue();
    [Fact] void should_not_report_generation_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_compile_the_baseline_application_source() => Analyzed.ErrorsIn(_baselineCompilation).ShouldBeEmpty();
    [Fact] void should_import_the_baseline_primary_target() => _baselineResult.Source.ShouldContain("import Partners.Contracts.UserRenamed");
    [Fact] void should_compile_the_baseline_document() => _baselineVerification.Compiles.ShouldBeTrue();
    [Fact] void should_round_trip_the_baseline_stably() => Verification.RoundTrip.For(_baselineVerification.Application!).IsStable.ShouldBeTrue();
    [Fact] void should_pin_the_baseline_binding_limits() => _baselineVerification.BindingDiagnostics.Select(_ => (_.Code, _.Message)).ShouldContainOnly([("PLAY0270", DomainMetadata), ("PLAY0268", PrimaryImportLimit), ("PLAY0390", PrimaryConstraintLimit)]);
    [Fact] void should_pin_the_baseline_standalone_diagnostic_treatment() => _baselineResult.Diagnostics.Select(_ => (_.Code, _.Severity)).ShouldContainOnly([("SP0056", ScreenplayDiagnosticSeverity.Warning), ("SP0056", ScreenplayDiagnosticSeverity.Warning)]);
}
