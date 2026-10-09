// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_showing_the_specifications_of_a_command : Specification
{
    EventModelView _view;
    Slice _slice;
    SliceSpecification _registering;
    SliceSpecification _rejecting;

    void Because()
    {
        var source = string.Join(
            '\n',
            "concept ProjectId : Uuid",
            "concept ReceiptId : Uuid",
            "concept ProjectName : String",
            "module Projects",
            "  feature Registration",
            "    slice StateChange Register",
            "      command RegisterProject",
            "        projectId ProjectId generated identifier",
            "        receiptId ReceiptId generated",
            "        name ProjectName",
            "        produces event ProjectRegistered",
            "          name ProjectName = name",
            "        returns",
            "          projectId = projectId",
            "          receiptId ReceiptId = receiptId",
            "      specification RegisteringReturnsIdentifiers",
            "        when RegisterProject",
            "          for \"11111111-1111-1111-1111-111111111111\"",
            "          generated receiptId = \"22222222-2222-2222-2222-222222222222\"",
            "          name = \"Apollo\"",
            "        then ProjectRegistered",
            "          for \"11111111-1111-1111-1111-111111111111\"",
            "          name = \"Apollo\"",
            "        then returns",
            "          receiptId = \"22222222-2222-2222-2222-222222222222\"",
            "      specification RejectingAnUnnamedProject",
            "        given ProjectRegistered",
            "          name = \"Apollo\"",
            "        when RegisterProject",
            "          name = \"\"",
            "        then error \"A project needs a name\"");
        _view = new EventModelParser().Parse("Projects", "Projects", source);
        _slice = _view.EventModel!.Collections[0].Modules[0].Features[0].Slices[0];
        _registering = _slice.Specifications[0];
        _rejecting = _slice.Specifications[1];
    }

    [Fact] void should_compile_the_document() => _view.Success.ShouldBeTrue();
    [Fact] void should_carry_both_specifications() => _slice.Specifications.Select(_ => _.Name.Split(' ')[0]).ShouldContainOnly("RegisteringReturnsIdentifiers", "RejectingAnUnnamedProject");
    [Fact] void should_run_the_slice_command_as_the_action() => _registering.When!.CommandId.ShouldEqual(_slice.Command!.Id);
    [Fact] void should_carry_the_command_values() => _registering.When!.Values["name"]!.GetValue<string>().ShouldEqual("Apollo");
    [Fact] void should_not_carry_generated_values_as_request_inputs() => _registering.When!.Values.ContainsKey("receiptId").ShouldBeFalse();
    [Fact] void should_name_the_generated_values() => _registering.Name.ShouldContain("generated (not request inputs): receiptId = \"22222222-2222-2222-2222-222222222222\"");
    [Fact] void should_name_the_expected_response() => _registering.Name.ShouldContain("then returns { receiptId = \"22222222-2222-2222-2222-222222222222\" }");
    [Fact] void should_point_the_expected_event_at_the_event_on_the_slice() => _registering.ThenEvents.Single().EventId.ShouldEqual(_slice.Events.Single(_ => _.Name == "ProjectRegistered").Id);
    [Fact] void should_carry_the_expected_event_values() => _registering.ThenEvents.Single().Values["name"]!.GetValue<string>().ShouldEqual("Apollo");
    [Fact] void should_name_the_source_the_expected_event_is_on() => _registering.ThenEvents.Single().Name.ShouldEqual("ProjectRegistered — for \"11111111-1111-1111-1111-111111111111\"");
    [Fact] void should_name_the_source_the_command_runs_for() => _registering.Name.ShouldContain("when for \"11111111-1111-1111-1111-111111111111\"");
    [Fact] void should_carry_the_given_event() => _rejecting.Given.Single().Name.ShouldEqual("ProjectRegistered");
    [Fact] void should_carry_the_expected_error_message() => _rejecting.ThenErrors.Single().Name.ShouldEqual("A project needs a name");
    [Fact] void should_keep_a_specification_without_details_named_as_declared() => _rejecting.Name.ShouldEqual("RejectingAnUnnamedProject");
    [Fact] void should_not_warn_about_dropped_specifications() => _view.Warnings.Any(_ => _.Message.Contains("specification", StringComparison.Ordinal)).ShouldBeFalse();
}
