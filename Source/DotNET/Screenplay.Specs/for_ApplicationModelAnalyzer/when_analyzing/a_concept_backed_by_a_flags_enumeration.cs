// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_concept_backed_by_a_flags_enumeration : Specification
{
    const string Source = """
        using System;
        using Cratis.Concepts;
        using Cratis.Chronicle.Events;
        namespace Library.Notifications.Sending;
        [Flags] public enum Channels { Email = 1, Sms = 2, InApp = 4 }
        public record PreferredChannels(Channels Value) : ConceptAs<Channels>(Value);
        [EventType] public record NotificationSent(PreferredChannels Channels);
        """;

    ApplicationModelAnalysis _result;

    void Because() => _result = Analyzed.Source(Source);

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_declare_the_wrapper_with_its_backing_numbers() => _result.Model.Concepts.Single().EnumNumbers.Select(number => (int)number).ShouldEqual([1, 2, 4]);
    [Fact] void should_name_the_backing_enum_and_property_in_the_warning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.FlagsCombinationsNotStatable).Message.ShouldContain("Enum 'Channels' is [Flags]; 'Library.Notifications.Sending.NotificationSent.Channels'");
}
