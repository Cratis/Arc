// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_strict_and_not_registered : given.a_lookup
{
    Exception _failure;

    void Establish() => Configure(new MissingCommand("key"), requireGeneratedMetadata: true);
    void Because() => _failure = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(MissingCommand).GetMethod(nameof(MissingCommand.Handle))!.GetParameters()[0], ReadProtectionMode.On));

    [Fact] void should_refuse_reflection() => _failure.ShouldBeOfExactType<CommandReadModelKeysMissing>();
    [Fact] void should_name_the_missing_type() => _failure.Message.ShouldContain(typeof(MissingCommand).ToString());

    public record MissingCommand(string Key)
    {
        public void Handle([LookUpBy(nameof(Key))] MissingModel model) { }
    }
    public record MissingModel(string Id);
}
