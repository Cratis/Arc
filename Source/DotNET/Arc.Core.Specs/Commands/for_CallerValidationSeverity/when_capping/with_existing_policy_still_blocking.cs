// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_existing_policy_still_blocking : Specification
{
    ValidationResult[] _explicitResults;
    ValidationResult[] _protectedResults;

    void Because()
    {
        _explicitResults = BlockingFor(typeof(ExplicitCommand));
        _protectedResults = BlockingFor(typeof(ProtectedCommand));
    }

    static ValidationResult[] BlockingFor(Type type)
    {
        var requested = CallerValidationSeverity.Cap(type, ValidationResultSeverity.Error, new CommandOptions());
        var policy = CommandValidationResults.ForCommand(type, requested);
        return CommandValidationResults.Blocking([new(ValidationResultSeverity.Error, "Error", [], new object()), new(ValidationResultSeverity.Unknown, "Unknown", [], new object())], policy.AllowedSeverity, policy.BlockUnknown).ToArray();
    }

    [Fact] void should_keep_errors_and_unknown_under_an_explicit_policy() => _explicitResults.Length.ShouldEqual(2);
    [Fact] void should_keep_errors_and_unknown_under_read_protection() => _protectedResults.Length.ShouldEqual(2);

    [AllowCallerValidationSeverity(ValidationResultSeverity.Error)]
    [BlockOnValidationSeverity(ValidationResultSeverity.Error)]
    public record ExplicitCommand;

    [AllowCallerValidationSeverity(ValidationResultSeverity.Error)]
    [ReadProtection(ReadProtectionMode.On)]
    public record ProtectedCommand;
}
