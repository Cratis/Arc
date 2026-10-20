// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_strict_and_the_model_is_not_registered : given.a_lookup
{
    Exception _failure;

    void Establish()
    {
        CommandReadModelKeys.RegisterCommand(typeof(RegisteredCommand), new CommandReadModelKeyProperty("Key", typeof(string), _ => "key"));
        Configure(new RegisteredCommand("key"), requireGeneratedMetadata: true);
    }

    void Because() => _failure = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(RegisteredCommand).GetMethod(nameof(RegisteredCommand.Handle))!.GetParameters()[0], ReadProtectionMode.On));

    [Fact] void should_refuse_missing_model_metadata() => _failure.ShouldBeOfExactType<CommandReadModelKeysMissing>();
    [Fact] void should_name_the_model() => _failure.Message.ShouldContain(typeof(MissingModel).ToString());

    public record RegisteredCommand(string Key)
    {
        public void Handle(MissingModel model) { }
    }
    public record MissingModel(string Id);
}
