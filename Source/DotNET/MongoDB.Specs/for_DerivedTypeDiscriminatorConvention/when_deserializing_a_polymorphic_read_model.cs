// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention;

/// <summary>
/// Round-trips a read model embedding a polymorphic element tree the way MongoDBDefaults wires it in a real
/// application: camel-cased element names, extra elements ignored, and DerivedTypeDiscriminatorConvention
/// registered for every type with derivatives. This is the exact shape that overflowed the stack in
/// production when the discriminator could not resolve a concrete type.
/// </summary>
public class when_deserializing_a_polymorphic_read_model : Specification
{
    static bool _registered;

    ModuleReadModel _original;
    ModuleReadModel _deserialized;

    void Establish()
    {
        if (!_registered)
        {
            _registered = true;

            var types = Substitute.For<ITypes>();
            types.All.Returns(
            [
                typeof(ButtonVisual),
                typeof(KnobVisual),
                typeof(NotEmptyRule)
            ]);
            var derivedTypes = new DerivedTypes(types);

            var convention = new DerivedTypeDiscriminatorConvention(derivedTypes);
            foreach (var type in derivedTypes.TypesWithDerivatives)
            {
                BsonSerializer.RegisterDiscriminatorConvention(type, convention);
            }

            var derivedTypePack = new ConventionPack { new DerivedTypeClassMapConvention(derivedTypes, convention) };
            ConventionRegistry.Register("polymorphic read model derived types", derivedTypePack, _ => true);

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register(
                "polymorphic read model specs",
                pack,
                type => type.Namespace == typeof(when_deserializing_a_polymorphic_read_model).Namespace);
        }

        _original = new ModuleReadModel(
            "m1",
            "My First Module",
            [
                new FeatureElement(
                    "f1",
                    "My First Feature",
                    [
                        new SliceElement(
                            "s1",
                            "Register",
                            [
                                new ActorElement(
                                    "a1",
                                    [
                                        new ButtonVisual
                                        {
                                            Name = "Button",
                                            Rounded = true,
                                            Severity = 2,
                                            Width = 120,
                                            Height = 42,
                                            Properties = new Dictionary<string, string> { ["tabIndex"] = "0" }
                                        },
                                        new KnobVisual { Name = "Knob", Size = 100 }
                                    ])
                            ],
                            new CommandElement(
                                "c1",
                                "Register",
                                [new RulesForProperty("name", [new NotEmptyRule()])]))
                    ])
            ]);
    }

    void Because()
    {
        var document = _original.ToBsonDocument();
        _deserialized = BsonSerializer.Deserialize<ModuleReadModel>(document);
    }

    [Fact] void should_round_trip_the_module_name() => _deserialized.Name.ShouldEqual(_original.Name);
    [Fact] void should_round_trip_the_element_count() => _deserialized.Features[0].Slices[0].Actors[0].Elements.Count.ShouldEqual(2);
    [Fact] void should_resolve_the_button_to_its_concrete_type() => _deserialized.Features[0].Slices[0].Actors[0].Elements[0].ShouldBeOfExactType<ButtonVisual>();
    [Fact] void should_resolve_the_knob_to_its_concrete_type() => _deserialized.Features[0].Slices[0].Actors[0].Elements[1].ShouldBeOfExactType<KnobVisual>();
    [Fact] void should_round_trip_button_state() => ((ButtonVisual)_deserialized.Features[0].Slices[0].Actors[0].Elements[0]).Rounded.ShouldBeTrue();
    [Fact] void should_resolve_the_rule_to_its_concrete_type() => _deserialized.Features[0].Slices[0].Command!.Rules![0].Rules[0].ShouldBeOfExactType<NotEmptyRule>();
}
