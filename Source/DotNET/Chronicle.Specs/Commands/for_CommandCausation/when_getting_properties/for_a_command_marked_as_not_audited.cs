// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties;

/// <summary>
/// Marking the command rather than each of its properties is the right answer when a command exists only to carry
/// secrets, and it stays right as properties are added to it later - which marking them one by one does not.
/// </summary>
public class for_a_command_marked_as_not_audited : Specification
{
    [NotAudited]
    record ResetCredentials(Guid UserId, string Password, string RecoveryCode);

    IDictionary<string, string> _properties;

    void Because() => _properties = CommandCausation.PropertiesFor(
        typeof(ResetCredentials),
        new ResetCredentials(Guid.NewGuid(), "hunter2", "0000-1111"));

    [Fact] void should_still_name_the_command() =>
        _properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(ResetCredentials));

    [Fact] void should_still_qualify_the_command_name() =>
        _properties[CommandCausation.CommandTypeFullNameProperty].ShouldEqual(typeof(ResetCredentials).FullName);

    [Fact] void should_record_none_of_its_values() =>
        _properties.Count.ShouldEqual(2);
}
