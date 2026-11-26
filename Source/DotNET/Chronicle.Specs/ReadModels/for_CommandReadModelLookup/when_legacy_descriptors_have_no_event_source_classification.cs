// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

[PerFactSpecificationLifecycle]
public class when_legacy_descriptors_have_no_event_source_classification : given.a_lookup
{
    void Establish() => CommandReadModelKeys.RegisterCommand(
        typeof(LegacyCommand),
        new CommandReadModelKeyProperty(nameof(LegacyCommand.Target), typeof(string), command => ((LegacyCommand)command).Target),
        new CommandReadModelKeyProperty(nameof(LegacyCommand.Optional), typeof(string), command => ((LegacyCommand)command).Optional));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void should_refuse_absent_event_source_targets(string? key)
    {
        Configure(new LegacyCommand(key, "valid"), requireGeneratedMetadata: true);

        var failure = Catch.Exception(() => Resolve(nameof(LegacyCommand.TargetModel)));

        failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void should_allow_absent_non_target_keys(string? key)
    {
        Configure(new LegacyCommand("target", key), requireGeneratedMetadata: true);

        var value = Resolve(nameof(LegacyCommand.OptionalModel));

        value.ShouldBeNull();
    }

    object? Resolve(string method) => CommandReadModelLookup.Resolve(
        _context,
        typeof(LegacyCommand).GetMethod(method)!.GetParameters()[0],
        ReadProtectionMode.On,
        ProtectedKeyTraversalForSpecs.Create(),
        out _);

    public record LegacyCommand([property: Cratis.Chronicle.Keys.Key] string? Target, string? Optional)
    {
        public void TargetModel([LookUpBy(nameof(Target))] Model? model) { }
        public void OptionalModel([LookUpBy(nameof(Optional))] Model? model) { }
    }

    public record Model(string Id);
}
