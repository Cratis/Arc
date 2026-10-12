// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_flags_enumeration_with_every_combination_named : Specification
{
    const string Source = """
        using System;
        using Cratis.Chronicle.Events;
        namespace Library.Notifications.Sending;
        [Flags] public enum Channels { None = 0, Email = 1, Sms = 2, All = 3 }
        [EventType] public record NotificationSent(Channels Channels);
        """;

    ApplicationModelAnalysis _result;

    void Because() => _result = Analyzed.Source(Source);

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_not_warn_about_unnamed_combinations() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.FlagsCombinationsNotStatable).ShouldBeEmpty();
}
