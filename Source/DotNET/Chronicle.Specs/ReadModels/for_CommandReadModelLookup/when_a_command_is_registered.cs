// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_a_command_is_registered : given.a_lookup
{
    object _key;

    void Establish()
    {
        CommandReadModelKeys.RegisterCommand(typeof(RegisteredCommand), new CommandReadModelKeyProperty("Name", typeof(string), _ => "typed-marker"));
        CommandReadModelKeys.DeclareModelKey(typeof(RegisteredModel), typeof(string));
        Configure(new RegisteredCommand("reflection-value"), requireGeneratedMetadata: true);
    }

    void Because() => _key = CommandReadModelLookup.Resolve(_context, typeof(RegisteredCommand).GetMethod(nameof(RegisteredCommand.Handle))!.GetParameters()[0], ReadProtectionMode.On, ProtectedKeyTraversalForSpecs.Create(), out _);

    [Fact] void should_use_the_registered_getter_and_model_key_type() => _key.ToString().ShouldEqual("typed-marker");
    [Fact] void should_not_warn_about_reflection() => _logger.Messages.ShouldBeEmpty();

    public record RegisteredCommand(string Name)
    {
        public void Handle(RegisteredModel model) { }
    }
    public record RegisteredModel(Guid Id);
}
