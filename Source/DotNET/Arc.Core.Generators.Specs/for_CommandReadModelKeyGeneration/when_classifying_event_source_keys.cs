// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_classifying_event_source_keys : Specification
{
    const string Models = """
        namespace Cratis.Chronicle.Keys
        {
            [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Parameter)]
            public sealed class KeyAttribute : System.Attribute { }
        }
        namespace Cratis.Chronicle.Events
        {
            public class EventSourceId { }
            public class EventSourceId<T> { }
        }
        namespace Targets
        {
            public class AccountId : Cratis.Chronicle.Events.EventSourceId<AccountId> { }
            public class DerivedId : Cratis.Chronicle.Events.EventSourceId { }
            public class Base
            {
                [Cratis.Chronicle.Keys.Key]
                public string Inherited { get; set; } = "target";
            }
            [Cratis.Arc.Commands.ModelBound.Command]
            public class Change : Base
            {
                public Change([Cratis.Chronicle.Keys.Key] string Constructor) => this.Constructor = Constructor;
                public string Constructor { get; }
                [Cratis.Chronicle.Keys.Key]
                public string Property { get; set; } = "target";
                public Cratis.Chronicle.Events.EventSourceId Direct { get; set; } = new();
                public Cratis.Chronicle.Events.EventSourceId<AccountId> Generic { get; set; } = new();
                public AccountId Account { get; set; } = new();
                public DerivedId Derived { get; set; } = new();
                public string? Optional { get; set; }
            }
        }
        """;

    Dictionary<string, bool?> _classification;

    void Because()
    {
        var (_, diagnostics, output) = CommandKeyCompilation.Compile("ClassifiedTargets", Models);
        diagnostics.ShouldBeEmpty();
        var assembly = ModelGraphWalkerCompilation.Load(output);
        var registered = (Array)CommandKeyCompilation.Registered(assembly, "Commands")[assembly.GetType("Targets.Change")!]!;
        _classification = registered.Cast<object>().ToDictionary(
            property => (string)property.GetType().GetProperty("Name")!.GetValue(property)!,
            property => (bool?)property.GetType().GetProperty("IsEventSourceKey")!.GetValue(property));
    }

    [Fact] void should_classify_property_keys() => _classification["Property"].ShouldEqual(true);
    [Fact] void should_classify_matching_constructor_keys() => _classification["Constructor"].ShouldEqual(true);
    [Fact] void should_classify_inherited_keys() => _classification["Inherited"].ShouldEqual(true);
    [Fact] void should_classify_direct_event_source_ids() => _classification["Direct"].ShouldEqual(true);
    [Fact] void should_classify_generic_event_source_ids() => _classification["Generic"].ShouldEqual(true);
    [Fact] void should_classify_generic_event_source_id_subtypes() => _classification["Account"].ShouldEqual(true);
    [Fact] void should_classify_event_source_id_subtypes() => _classification["Derived"].ShouldEqual(true);
    [Fact] void should_allow_absence_for_non_target_keys() => _classification["Optional"].ShouldEqual(false);
}
