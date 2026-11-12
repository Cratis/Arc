// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_write_scopes_are_invalid : a_catalog_context
{
    Exception? _blank;
    Exception? _repeated;
    Exception? _readScope;
    Exception? _readScopeAsWriteScope;
    Exception? _writeScopeListedAgain;

    void Because()
    {
        _blank = Catch.Exception(() => BuildWith(" "));
        _repeated = Catch.Exception(() => BuildWith("notes:publish", "notes:publish"));
        _readScope = Catch.Exception(() => BuildWith("app:read"));
        _readScopeAsWriteScope = Catch.Exception(() => McpToolCatalog.Build(_exposure, new McpOptions { ReadScope = "same", WriteScope = "same" }, _commands, _queries));
        _writeScopeListedAgain = Catch.Exception(() => BuildWith("app:write"));
    }

    [Fact] void should_refuse_a_blank_write_scope() => _blank.ShouldBeOfExactType<McpOptionsInvalid>();
    [Fact] void should_refuse_a_repeated_write_scope() => _repeated.ShouldBeOfExactType<McpOptionsInvalid>();
    [Fact] void should_refuse_the_read_scope_as_a_write_scope() => _readScope.ShouldBeOfExactType<McpOptionsInvalid>();
    [Fact] void should_refuse_a_write_scope_equal_to_the_read_scope() => _readScopeAsWriteScope.ShouldBeOfExactType<McpOptionsInvalid>();
    [Fact] void should_accept_the_write_scope_listed_again() => _writeScopeListedAgain.ShouldBeNull();

    McpToolCatalog BuildWith(params string[] writeScopes)
    {
        var options = new McpOptions { ApplicationName = "app" };
        foreach (var scope in writeScopes)
        {
            options.WriteScopes.Add(scope);
        }

        return McpToolCatalog.Build(_exposure, options, _commands, _queries);
    }
}
