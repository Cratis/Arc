// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_executed_typed_capture : a_generated_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_append_once_with_a_typed_source_and_project_the_local_event(bool keyIsEventSource, bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, typed_capture_source.Files(keyIsEventSource));
        AssertDocument();
        var compiled = SemanticExecutionPlan.Compile(Bound.Value!.Model);
        compiled.Diagnostics.ShouldBeEmpty();
        var plan = compiled.Plan!;
        var placed = plan.Events.Values.Single(@event => @event.Name == "OrderPlaced");
        var received = plan.Events.Values.Single(@event => @event.Name == "OrderReceived");
        var key = SemanticValue.Text("19a06b87-1402-401c-bf54-141305648ee8");
        var fact = new SemanticFact(placed.Id, key, [new(placed.Properties.Single().Id, key)])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), key))
        };
        var execution = new SemanticReactionLoop(new SemanticEvaluator(), plan, SemanticWorld.Empty);

        Deliver(execution, fact);
        AssertLocalEffect(execution, received, placed.Properties.Single().Type, key);
        Deliver(execution, fact);
        AssertLocalEffect(execution, received, placed.Properties.Single().Type, key);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_an_event_source_that_disagrees_with_the_declared_key(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, typed_capture_source.Files(keyIsEventSource: true));
        AssertDocument();
        var plan = SemanticExecutionPlan.Compile(Bound.Value!.Model).Plan!;
        var placed = plan.Events.Values.Single(@event => @event.Name == "OrderPlaced");
        var fact = new SemanticFact(placed.Id, SemanticValue.Text("41a4d32c-fbd3-49b8-af13-8cbf30c40278"),
            [new(placed.Properties.Single().Id, SemanticValue.Text("19a06b87-1402-401c-bf54-141305648ee8"))]);
        var execution = new SemanticReactionLoop(new SemanticEvaluator(), plan, SemanticWorld.Empty);

        Should.Throw<InvalidSemanticContract>(() => execution.Append(fact, null));
        execution.World.Facts.ShouldBeEmpty();
        execution.World.Instances.ShouldBeEmpty();
    }

    static void Deliver(SemanticReactionLoop execution, SemanticFact fact)
    {
        execution.Append(fact, null).ShouldBeNull();
        execution.Settle().ShouldBeNull();
    }

    static void AssertLocalEffect(SemanticReactionLoop execution, SemanticEventContract received, SemanticTypeReference identity, SemanticValue key)
    {
        var local = execution.World.Facts.Single(fact => fact.EventContract == received.Id);
        local.Destination.ShouldEqual(key);
        local.Context!.EventSource.Type.ShouldEqual(identity);
        local.Context.EventSource.Value.ShouldEqual(key);
        local.Values.Single(value => value.TargetProperty == received.Properties.Single().Id).Value.ShouldEqual(key);
        var instance = execution.World.Instances.Single();
        instance.Key.ShouldEqual(key);
        instance.Values.Single().Value.ShouldEqual(key);
    }
}
