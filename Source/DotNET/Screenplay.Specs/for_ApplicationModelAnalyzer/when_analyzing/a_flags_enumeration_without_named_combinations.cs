// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_flags_enumeration_without_named_combinations : Specification
{
    const string Source = """
        using System;
        using Cratis.Chronicle.Events;
        namespace Library.Notifications.Sending;
        [Flags] public enum Channels { None = 0, Email = 1, Sms = 2, InApp = 4 }
        [EventType] public record NotificationSent(Channels Channels, Delivery[] Deliveries);
        public record Delivery(Channels Channels);
        """;

    ApplicationModelAnalysis _result;
    ScreenplayDiagnostic _warning;

    void Because()
    {
        _result = Analyzed.Source(Source);
        _warning = _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.FlagsCombinationsNotStatable);
    }

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_keep_every_member() => _result.Model.Concepts.Single().EnumValues.ShouldEqual(["None", "Email", "Sms", "InApp"]);
    [Fact] void should_warn_at_type_extraction() => _warning.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_list_direct_and_nested_uses_in_order() => _warning.Message.ShouldContain("'Library.Notifications.Sending.Delivery.Channels', 'Library.Notifications.Sending.NotificationSent.Channels'");
    [Fact] void should_explain_the_limitation() => _warning.Message.ShouldContain("combinations no member names cannot be stated (Screenplay#319)");
    [Fact] void should_locate_the_enum() => _warning.Location.ShouldEqual("Library.Notifications.Sending.Channels");
}
