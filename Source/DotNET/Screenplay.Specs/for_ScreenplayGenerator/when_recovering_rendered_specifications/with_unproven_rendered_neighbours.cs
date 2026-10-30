// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications;

public class with_unproven_rendered_neighbours : given.a_rendered_single_event_command
{
    readonly Dictionary<string, ScreenplayGenerationResult> _results = [];

    void Because()
    {
        foreach (var (name, scenario, slice, common) in Cases())
        {
            Generate(Options, given.RenderedEnumCommandSources.Sources(scenario, slice, common));
            AssertDocument();
            _results.Add(name, Result);
        }
    }

    [Fact] void should_reject_a_mutable_field_even_without_writes() => AssertUnread("mutable");
    [Fact] void should_reject_a_static_constructor_overwrite() => AssertUnread("overwrite");
    [Fact] void should_reject_a_ref_write() => AssertUnread("ref");
    [Fact] void should_reject_an_out_write() => AssertUnread("out");
    [Fact] void should_reject_a_chained_alias() => AssertUnread("alias");
    [Fact] void should_reject_computation() => AssertUnread("computed");
    [Fact] void should_reject_a_cast_initializer() => AssertUnread("cast");
    [Fact] void should_reject_a_getter() => AssertUnread("getter");
    [Fact] void should_reject_a_foreign_field_with_the_same_name() => AssertUnread("foreign");
    [Fact] void should_reject_the_wrong_enum() => AssertUnread("enum");
    [Fact] void should_reject_a_reassigned_readonly_scenario() => AssertUnread("reassigned");
    [Fact] void should_reject_a_different_scenario_receiver() => AssertUnread("receiver");
    [Fact] void should_reject_an_existence_only_assertion() => AssertUnread("existence");
    [Fact] void should_reject_a_second_existence_witness() => AssertUnread("duplicate");
    [Fact] void should_reject_an_incomplete_event() => AssertUnread("incomplete");
    [Fact] void should_reject_different_ids() => AssertUnread("ids");
    [Fact] void should_reject_a_conditional_assertion() => AssertUnread("conditional");
    [Fact] void should_reject_a_disjunction() => AssertUnread("disjunction");
    [Fact] void should_reject_a_nontransparent_uuid_concept() => AssertUnread("uuid");
    [Fact] void should_reject_a_name_collision() => AssertUnread("collision");
    [Fact] void should_reject_an_inherited_field() => AssertUnread("inherited");
    [Fact] void should_reject_an_operator_initializer() => AssertUnread("operator");
    [Fact] void should_not_combine_independent_predicates() => AssertUnread("split");
    [Fact] void should_reject_a_computed_identity() => AssertUnread("identity");
    [Fact] void should_not_widen_a_legacy_scenario() => AssertUnread("legacy");
    [Fact] void should_reject_an_arbitrary_helper() => AssertUnread("helper");
    [Fact] void should_reject_an_early_return_before_the_complete_assertion() => AssertUnread("early-complete");
    [Fact] void should_reject_an_early_return_before_the_existence_assertion() => AssertUnread("early-existence");
    [Fact] void should_reject_a_swallowed_complete_assertion_failure() => AssertUnread("caught-complete");
    [Fact] void should_reject_a_swallowed_existence_assertion_failure() => AssertUnread("caught-existence");
    [Fact] void should_reject_an_early_return_before_execution() => AssertUnread("early-execution");
    [Fact] void should_reject_a_swallowed_execution_failure() => AssertUnread("caught-execution");
    [Fact] void should_reject_an_independent_nonforwarding_assertion_source() => AssertUnread("transformed-source");

    IEnumerable<(string Name, string Scenario, string Slice, string Common)> Cases()
    {
        yield return Change("mutable", "static InvoiceStatus _status = InvoiceStatus.Draft;");
        yield return Change("overwrite", StatusField + " static when_draft_status() { _status = InvoiceStatus.Issued; }");
        yield return Change("ref", StatusField + " static when_draft_status() { Change(ref _status); } static void Change(ref InvoiceStatus value) => value = InvoiceStatus.Issued;");
        yield return Change("out", StatusField + " static when_draft_status() { Change(out _status); } static void Change(out InvoiceStatus value) => value = InvoiceStatus.Issued;");
        yield return Change("alias", "static readonly InvoiceStatus _other = InvoiceStatus.Draft; static readonly InvoiceStatus _status = _other;");
        yield return Change("computed", "static readonly InvoiceStatus _status = Compute(); static InvoiceStatus Compute() => InvoiceStatus.Draft;");
        yield return Change("cast", "static readonly InvoiceStatus _status = (InvoiceStatus)(-1);");
        yield return Change("getter", "static InvoiceStatus _status => InvoiceStatus.Draft;");
        yield return Change("operator", "static readonly InvoiceStatus _status = InvoiceStatus.Draft | InvoiceStatus.Issued;");
        yield return ("foreign", Scenario.Replace(StatusField, StatusField + " class Foreign { public static readonly InvoiceStatus _status = InvoiceStatus.Draft; }", StringComparison.Ordinal)
            .Replace("new RecordStatus(_id, _status)", "new RecordStatus(_id, Foreign._status)", StringComparison.Ordinal)
            .Replace("@event.Status == _status", "@event.Status == Foreign._status", StringComparison.Ordinal), Slice, Common);
        yield return ("enum", Scenario.Replace(StatusField, "enum OtherStatus { Draft = -1 } static readonly OtherStatus _status = OtherStatus.Draft;", StringComparison.Ordinal)
            .Replace("new RecordStatus(_id, _status)", "new RecordStatus(_id, (InvoiceStatus)_status)", StringComparison.Ordinal)
            .Replace("@event.Status == _status", "@event.Status == (InvoiceStatus)_status", StringComparison.Ordinal), Slice, Common);
        yield return ("reassigned", Scenario.Replace(StatusField, StatusField + " public when_draft_status() { _scenario = new(); }", StringComparison.Ordinal), Slice, Common);
        yield return ("receiver", Scenario.Replace(StatusField, StatusField + " readonly CommandScenario<RecordStatus> _other = new();", StringComparison.Ordinal)
            .Replace(CompleteAssertion, CompleteAssertion.Replace("_scenario.", "_other.", StringComparison.Ordinal), StringComparison.Ordinal), Slice, Common);
        yield return ("existence", Scenario.Replace(CompleteAssertion, string.Empty, StringComparison.Ordinal), Slice, Common);
        yield return ("duplicate", Scenario.Replace(ExistenceAssertion, ExistenceAssertion + " [Fact] async Task should_also_append() => await _scenario.ShouldHaveAppendedEvent<RecordStatus, StatusRecorded>(_id, @event => true);", StringComparison.Ordinal), Slice, Common);
        yield return ("incomplete", Scenario, Slice.Replace("StatusRecorded(InvoiceStatus Status);", "StatusRecorded(InvoiceStatus Status, string Note);", StringComparison.Ordinal)
            .Replace("new(Status)", "new(Status, \"note\")", StringComparison.Ordinal)
            .Replace("new(InvoiceStatus.Issued)", "new(InvoiceStatus.Issued, \"note\")", StringComparison.Ordinal), Common);
        yield return ("ids", Scenario.Replace(ExistenceAssertion, ExistenceAssertion.Replace("(_id,", "(new InvoiceId(Guid.Parse(\"5fa85f64-5717-4562-b3fc-2c963f66afa6\")),", StringComparison.Ordinal), StringComparison.Ordinal), Slice, Common);
        yield return ("conditional", Scenario.Replace(CompleteAssertion, CompleteAssertion.Replace("=> await", "{ if (_result != null) await", StringComparison.Ordinal) + " }", StringComparison.Ordinal), Slice, Common);
        yield return ("disjunction", Scenario.Replace("@event.Status == _status", "@event.Status == _status || @event.Status == InvoiceStatus.Issued", StringComparison.Ordinal), Slice, Common);
        yield return ("uuid", Scenario, Slice, Common.Replace("EventSourceId<Guid>(Value)", "EventSourceId<Guid>(Guid.Empty)", StringComparison.Ordinal));
        yield return ("collision", Scenario, Slice + "\npublic class DraftStatus { }", Common);
        yield return ("identity", Scenario.Replace("Guid.Parse(\"3fa85f64-5717-4562-b3fc-2c963f66afa6\")", "Guid.NewGuid()", StringComparison.Ordinal), Slice, Common);
        yield return ("legacy", Scenario.Replace("when_draft_status", "legacy_scenario", StringComparison.Ordinal), Slice, Common);
        yield return ("helper", Scenario.Replace("=> _result = await _scenario.Execute(new RecordStatus(_id, _status));", "{ Helper(); _result = await _scenario.Execute(new RecordStatus(_id, _status)); } static void Helper() { }", StringComparison.Ordinal), Slice, Common);
        yield return ("split", Scenario.Replace("@event => true", "@event => @event.Note == \"note\"", StringComparison.Ordinal),
            Slice.Replace("StatusRecorded(InvoiceStatus Status);", "StatusRecorded(InvoiceStatus Status, string Note);", StringComparison.Ordinal)
                .Replace("new(Status)", "new(Status, \"note\")", StringComparison.Ordinal)
                .Replace("new(InvoiceStatus.Issued)", "new(InvoiceStatus.Issued, \"note\")", StringComparison.Ordinal), Common);
        yield return ("early-complete", Scenario.Replace(CompleteAssertion, CompleteAssertion.Replace("=> await", "{ if (_result != null) return; await", StringComparison.Ordinal) + " }", StringComparison.Ordinal), Slice, Common);
        yield return ("early-existence", Scenario.Replace(ExistenceAssertion, ExistenceAssertion.Replace("=> await", "{ if (_result != null) return; await", StringComparison.Ordinal) + " }", StringComparison.Ordinal), Slice, Common);
        yield return ("caught-complete", Scenario.Replace(CompleteAssertion, CompleteAssertion.Replace("=> await", "{ try { await", StringComparison.Ordinal) + " } catch { } }", StringComparison.Ordinal), Slice, Common);
        yield return ("caught-existence", Scenario.Replace(ExistenceAssertion, ExistenceAssertion.Replace("=> await", "{ try { await", StringComparison.Ordinal) + " } catch { } }", StringComparison.Ordinal), Slice, Common);
        yield return ("early-execution", Scenario.Replace("=> _result = await", "{ if (_result != null) return; _result = await", StringComparison.Ordinal)
            .Replace("new RecordStatus(_id, _status));", "new RecordStatus(_id, _status)); }", StringComparison.Ordinal), Slice, Common);
        yield return ("caught-execution", Scenario.Replace("=> _result = await", "{ try { _result = await", StringComparison.Ordinal)
            .Replace("new RecordStatus(_id, _status));", "new RecordStatus(_id, _status)); } catch { } }", StringComparison.Ordinal), Slice, Common);
        yield return ("transformed-source", Scenario.Replace(StatusField, StatusField + " static readonly InvoiceId _expectedId = new InvoiceId(\"" + given.RenderedEnumCommandSources.Identity + "\");", StringComparison.Ordinal)
            .Replace("(_id, @event", "(_expectedId, @event", StringComparison.Ordinal), Slice,
            Common.Replace("public static readonly InvoiceId NotSet", "public InvoiceId(string value) : this(Guid.Empty) { } public static readonly InvoiceId NotSet", StringComparison.Ordinal));
        yield return ("inherited", Scenario.Replace("Specification, IDisposable", "Fixture, IDisposable", StringComparison.Ordinal)
            .Replace(StatusField, string.Empty, StringComparison.Ordinal) + "\npublic class Fixture : Specification { protected static readonly InvoiceStatus _status = InvoiceStatus.Draft; }", Slice, Common);
    }

    (string Name, string Scenario, string Slice, string Common) Change(string name, string field) =>
        (name, Scenario.Replace(StatusField, field, StringComparison.Ordinal), Slice, Common);

    void AssertUnread(string name)
    {
        var result = _results[name];
        result.Model.Slices.SelectMany(slice => slice.Specifications).ShouldBeEmpty();
        result.Source.ShouldNotContain("specification ");
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "SP0039" && !string.IsNullOrWhiteSpace(diagnostic.Location));
    }
}
