// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandSensitiveValueAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandSensitiveValueAnalyzer.when_validating_a_command;

/// <summary>
/// The guess is on words, not fragments. An analyzer that reports "Passenger" because it starts with "Pass" gets
/// suppressed wholesale, and the real findings go with it.
/// </summary>
public class and_a_name_merely_contains_a_sensitive_fragment : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record BookingRegistered(Guid BookingId);

    [Command]
    public record RegisterBooking(Guid BookingId, string PassengerName, decimal Subtotal, string Pinboard)
    {
        public BookingRegistered Handle() => new(BookingId);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
