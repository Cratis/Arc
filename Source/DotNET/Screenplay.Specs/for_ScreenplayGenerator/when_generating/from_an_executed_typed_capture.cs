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
        compiled.Issues.ShouldBeEmpty();
        var plan = compiled.Plan!;
        var placed = plan.Events.Values.Single(@event => @event.Name == "OrderPlaced");
        var received = plan.Events.Values.Single(@event => @event.Name == "OrderReceived");
        var key = SemanticValue.Text("19a06b87-1402-401c-bf54-141305648ee8");
        var fact = new SemanticFact(placed.Id, key, [new(placed.Properties.Single().Id, key)])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), key))
        };
        var world = Deliver(plan, SemanticWorld.Empty, fact);
        AssertLocalEffect(world, received, placed.Properties.Single().Type, key);
        world = Deliver(plan, world, fact);
        AssertLocalEffect(world, received, placed.Properties.Single().Type, key);
        world.Facts.Count(@event => @event.EventContract == received.Id).ShouldEqual(1);

        var otherKey = SemanticValue.Text("41a4d32c-fbd3-49b8-af13-8cbf30c40278");
        var other = fact with
        {
            Destination = otherKey,
            Values = [new(placed.Properties.Single().Id, otherKey)],
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), otherKey))
        };
        world = Deliver(plan, world, other);
        world = Deliver(plan, world, fact);
        world = Deliver(plan, world, other);
        world.Facts.Count(@event => @event.EventContract == received.Id).ShouldEqual(2);
        world.ReadModels.Length.ShouldEqual(2);
        AssertLocalEffect(world, received, placed.Properties.Single().Type, key);
        AssertLocalEffect(world, received, placed.Properties.Single().Type, otherKey);
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
        var source = SemanticValue.Text("41a4d32c-fbd3-49b8-af13-8cbf30c40278");
        var fact = new SemanticFact(placed.Id, source, [new(placed.Properties.Single().Id, SemanticValue.Text("19a06b87-1402-401c-bf54-141305648ee8"))])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), source))
        };
        var evaluator = new SemanticEvaluator();
        var appended = Assert.IsType<SemanticAccepted>(evaluator.ExtendWorld(plan, SemanticWorld.Empty, [fact]));

        Assert.Throws<InvalidSemanticContract>(() => new SemanticReactionSettlement(evaluator).Settle(plan, appended.World, appended.Facts, null));
        Assert.Single(appended.World.Facts);
        appended.World.ReadModels.ShouldBeEmpty();
    }

    static SemanticWorld Deliver(SemanticExecutionPlan plan, SemanticWorld world, SemanticFact fact)
    {
        var evaluator = new SemanticEvaluator();
        var appended = Assert.IsType<SemanticAccepted>(evaluator.ExtendWorld(plan, world, [fact]));
        return Assert.IsType<SemanticAccepted>(new SemanticReactionSettlement(evaluator).Settle(plan, appended.World, appended.Facts, null)).World;
    }

    static void AssertLocalEffect(SemanticWorld world, SemanticEventContract received, SemanticTypeReference identity, SemanticValue key)
    {
        var local = world.Facts.Single(fact => fact.EventContract == received.Id && fact.Destination == key);
        local.Destination.ShouldEqual(key);
        local.Context!.EventSource.Type.ShouldEqual(identity);
        local.Context.EventSource.Value.ShouldEqual(key);
        local.Values.Single(value => value.TargetProperty == received.Properties.Single().Id).Value.ShouldEqual(key);
        var instance = world.ReadModels.Single(instance => instance.Key == key);
        instance.Key.ShouldEqual(key);
        instance.Values.Single().Value.ShouldEqual(key);
    }
}
