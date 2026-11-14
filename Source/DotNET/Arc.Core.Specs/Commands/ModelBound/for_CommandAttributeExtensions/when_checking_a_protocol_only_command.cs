// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.ModelBound.for_CommandAttributeExtensions;

public class when_checking_a_protocol_only_command : Specification
{
    bool _isProtocolOnly;
    bool _isCommand;
    bool _unmarkedIsProtocolOnly;
    bool _derivedIsProtocolOnly;

    void Because()
    {
        _isProtocolOnly = typeof(ProtocolCommand).IsProtocolOnly();
        _isCommand = typeof(ProtocolCommand).IsCommand();
        _unmarkedIsProtocolOnly = typeof(PublicCommand).IsProtocolOnly();
        _derivedIsProtocolOnly = typeof(DerivedCommand).IsProtocolOnly();
    }

    [Fact] void should_recognize_the_marker() => _isProtocolOnly.ShouldBeTrue();
    [Fact] void should_still_recognize_the_command() => _isCommand.ShouldBeTrue();
    [Fact] void should_not_mark_an_ordinary_command() => _unmarkedIsProtocolOnly.ShouldBeFalse();
    [Fact] void should_not_inherit_the_marker() => _derivedIsProtocolOnly.ShouldBeFalse();

    [Command]
    [ProtocolOnly]
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
