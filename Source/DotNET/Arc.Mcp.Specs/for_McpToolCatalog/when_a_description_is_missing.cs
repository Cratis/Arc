// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_a_description_is_missing : a_catalog_context
{
    Exception? _blank;
    Exception? _attribute;
    Exception? _control;

    void Establish() => AddHandlerFor<AttributedCommandWithoutDescription>();

    void Because()
    {
        _blank = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<DeclaredCommand>("declared_command", "  "), _options, _commands, _queries));
        _attribute = Catch.Exception(() => McpToolCatalog.Build(new McpExposure(), _options, _commands, _queries));
        _handlers.Remove(typeof(AttributedCommandWithoutDescription));
        _control = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<DeclaredCommand>("declared_command", "Described."), _options, _commands, _queries));
    }

    [Fact] void should_refuse_a_blank_description() => _blank.ShouldBeOfExactType<McpToolDescriptionMissing>();
    [Fact] void should_refuse_an_attribute_without_a_description() => _attribute.ShouldBeOfExactType<McpToolDescriptionMissing>();
    [Fact] void should_accept_a_described_tool() => _control.ShouldBeNull();
}
