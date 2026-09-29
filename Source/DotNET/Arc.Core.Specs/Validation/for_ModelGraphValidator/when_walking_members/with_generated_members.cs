// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// A type with members registered by the Arc source generator is walked through those members and their getters
/// rather than through reflection, while a type without a registration below it is still walked through reflection.
/// </summary>
public class with_generated_members : given.a_recording_model_graph_validator
{
    /// <summary>
    /// Counts reads across instances: the traversal caches the members of a type for the process, so the getter
    /// registered first is the one read.
    /// </summary>
    static int _reads;
    int _readsBefore;

    void Establish()
    {
        _readsBefore = _reads;
        ModelGraphWalkers.Register(typeof(Model), [
            new ModelGraphMember("Child", instance =>
            {
                _reads++;
                return ((Model)instance).Child;
            })
        ]);
    }

    void Because() => Walk(new Model(new Child("child"), "not registered"));

    [Fact] void should_walk_only_the_registered_members_and_reflect_below_them() => Traversal.ShouldEqual("Model@ | Child@child | String@child.name");
    [Fact] void should_read_through_the_registered_getter() => (_reads - _readsBefore).ShouldEqual(1);

    record Child(string Name);

    record Model(Child Child, string Unregistered);
}
