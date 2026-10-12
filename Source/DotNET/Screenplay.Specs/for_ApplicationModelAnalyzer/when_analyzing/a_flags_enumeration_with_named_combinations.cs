// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_flags_enumeration_with_named_combinations : Specification
{
    const string Source = """
        using System;
        using Cratis.Chronicle.Events;
        namespace Library.Notifications.Sending;
        [Flags] public enum Channels { None = 0, Email = 1, Sms = 2, InApp = 4, All = 7 }
        [EventType] public record NotificationSent(Channels Channels);
        """;

    ApplicationModelAnalysis _result;

    void Because() => _result = Analyzed.Source(Source);

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_keep_the_named_combination() => _result.Model.Concepts.Single().EnumValues.ShouldContain("All");
    [Fact] void should_preserve_the_combination_number() => _result.Model.Concepts.Single().EnumNumbers.Select(number => (int)number).ShouldEqual([0, 1, 2, 4, 7]);
    [Fact] void should_explain_named_combinations_are_ordinary_members() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.FlagsCombinationsNotStatable).Message.ShouldContain("named combinations 'all' are declared as ordinary members");
}
