// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_a_command_is_protocol_only : for_MetadataQueryAttributes.given.a_metadata_discovery
{
    bool _isProtocolOnly;
    bool _isCommand;
    bool _publicIsProtocolOnly;
    bool _derivedIsProtocolOnly;

    void Because()
    {
        var command = MetadataType<ProtocolCommand>();
        _isProtocolOnly = command.IsProtocolOnly();
        _isCommand = command.IsCommand();
        _publicIsProtocolOnly = MetadataType<PublicCommand>().IsProtocolOnly();
        _derivedIsProtocolOnly = MetadataType<DerivedCommand>().IsProtocolOnly();
    }

    [Fact] void should_recognize_the_marker_in_metadata() => _isProtocolOnly.ShouldBeTrue();
    [Fact] void should_still_recognize_the_command() => _isCommand.ShouldBeTrue();
    [Fact] void should_not_exclude_an_ordinary_command() => _publicIsProtocolOnly.ShouldBeFalse();
    [Fact] void should_not_inherit_the_marker() => _derivedIsProtocolOnly.ShouldBeFalse();

    [Command]
    [Cratis.Arc.Commands.ModelBound.ProtocolOnly]
    public record ProtocolCommand
    {
        public void Handle() { }
    }

    [Command]
    public record PublicCommand
    {
        public void Handle() { }
    }

    public record DerivedCommand : ProtocolCommand;
}
