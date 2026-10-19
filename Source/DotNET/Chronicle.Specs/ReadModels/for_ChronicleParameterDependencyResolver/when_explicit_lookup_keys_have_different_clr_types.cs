// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Concepts;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_explicit_lookup_keys_have_different_clr_types
{
    static readonly Guid _identifier = Guid.NewGuid();
    static readonly string _key = _identifier.ToString("D");

    [Theory]
    [InlineData(typeof(Identifier), typeof(string))]
    [InlineData(typeof(string), typeof(Identifier))]
    [InlineData(typeof(Source), typeof(EventSourceId))]
    [InlineData(typeof(EventSourceId), typeof(Source))]
    [InlineData(typeof(Guid), typeof(string))]
    [InlineData(typeof(string), typeof(Guid))]
    [InlineData(typeof(GuidIdentifier), typeof(Guid))]
    [InlineData(typeof(Guid), typeof(GuidIdentifier))]
    [InlineData(typeof(object), typeof(Guid))]
    public void should_canonicalize_the_property_without_requiring_the_model_id_type(Type propertyType, Type modelKeyType)
    {
        var value = propertyType == typeof(string) ? _key : propertyType == typeof(Guid) ? _identifier :
            propertyType == typeof(GuidIdentifier) ? new GuidIdentifier(_identifier) :
            propertyType == typeof(object) ? new WireKey() : Activator.CreateInstance(propertyType, _key)!;
        var commandType = typeof(LookupCommand<>).MakeGenericType(propertyType);
        var command = Activator.CreateInstance(commandType, value)!;
        var context = new CommandContext(CorrelationId.New(), commandType, command, [], new());
        var parameter = typeof(Dependency<>).MakeGenericType(modelKeyType).GetMethod(nameof(Dependency<string>.Handle))!.GetParameters()[0];

        foreach (var mode in new[] { ReadProtectionMode.Off, ReadProtectionMode.Measure, ReadProtectionMode.On })
        {
            CommandReadModelLookup.Resolve(context, parameter, mode, ProtectedKeyTraversalForSpecs.Create(), out _).Value.ShouldEqual(_key);
        }
    }

    [Fact]
    public void should_format_a_numeric_property_for_a_string_model_key()
    {
        var command = new LookupCommand<int>(42);
        var context = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new());
        var parameter = typeof(Dependency<string>).GetMethod(nameof(Dependency<string>.Handle))!.GetParameters()[0];
        CommandReadModelLookup.Resolve(context, parameter, ReadProtectionMode.Off).Value.ShouldEqual("42");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void should_still_refuse_an_unspecified_key(string? value)
    {
        var command = new LookupCommand<string?>(value);
        var context = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new());
        var parameter = typeof(Dependency<string>).GetMethod(nameof(Dependency<string>.Handle))!.GetParameters()[0];
        var failure = Catch.Exception(() => CommandReadModelLookup.Resolve(context, parameter, ReadProtectionMode.On, ProtectedKeyTraversalForSpecs.Create(), out _));
        failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
        ((IValidationFailure)failure).ValidationResult.Reason.ShouldEqual(ValidationResultReason.Rule);
        ((IValidationFailure)failure).ValidationResult.Message.ShouldEqual($"{typeof(Model<string>).Name} '' does not exist.");
        foreach (var mode in new[] { ReadProtectionMode.Off, ReadProtectionMode.Measure })
        {
            Catch.Exception(() => CommandReadModelLookup.Resolve(context, parameter, mode)).ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
        }
    }

    public record WireKey
    {
        public override string ToString() => _key;
    }
    public record Identifier(string Value) : ConceptAs<string>(Value);
    public record GuidIdentifier(Guid Value) : ConceptAs<Guid>(Value);
    public record Source(string Value) : EventSourceId(Value);
    public record LookupCommand<T>(T Key);
    public record Model<T>(T Id);
    public class Dependency<T>
    {
        public void Handle([LookUpBy("Key")] Model<T> model) { }
    }
}
