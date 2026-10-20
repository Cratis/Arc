// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_falling_back_to_reflection : given.a_lookup
{
    object _key;

    void Establish() => Configure(new ReflectionCommand("sensitive-key"));

    void Because()
    {
        var parameter = typeof(ReflectionCommand).GetMethod(nameof(ReflectionCommand.Handle))!.GetParameters()[0];
        _key = CommandReadModelLookup.Resolve(_context, parameter, ReadProtectionMode.Off);
        CommandReadModelLookup.Resolve(_context, parameter, ReadProtectionMode.Off);
    }

    [Fact] void should_resolve_with_one_warning_without_key_values()
    {
        _key.ToString().ShouldEqual("sensitive-key");
        var warning = _logger.Messages.Single();
        warning.ShouldContain(typeof(ReflectionCommand).FullName!);
        warning.ShouldContain("ReadProtection:RequireGeneratedMetadata");
        warning.ShouldNotContain("sensitive-key");
    }

    public record ReflectionCommand(string Key)
    {
        public void Handle([LookUpBy(nameof(Key))] ReflectionModel model) { }
    }
    public record ReflectionModel(string Id);
}
