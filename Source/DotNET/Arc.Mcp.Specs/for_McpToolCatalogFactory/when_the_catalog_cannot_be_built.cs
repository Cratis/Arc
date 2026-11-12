// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp.for_McpToolCatalogFactory;

public class when_the_catalog_cannot_be_built : a_catalog_context
{
    McpToolCatalogFactory _broken;
    McpToolCatalogFactory _unscoped;
    McpToolCatalogFactory _valid;
    Exception? _first;
    Exception? _second;
    Exception? _noScope;
    Exception? _control;

    void Establish()
    {
        _broken = new McpToolCatalogFactory(
            new McpExposure().Expose<DeclaredCommand>("same_name", "One.").ExposeQuery<ReadModelWithQueries>(nameof(ReadModelWithQueries.Everything), "same_name", "Two."),
            Options.Create(_options),
            _commands,
            _queries,
            []);
        _unscoped = new McpToolCatalogFactory(
            new McpExposure().Expose<DeclaredCommand>("declared_command", "One."),
            Options.Create(new McpOptions()),
            _commands,
            _queries,
            []);
        _valid = new McpToolCatalogFactory(
            new McpExposure().Expose<DeclaredCommand>("declared_command", "One."),
            Options.Create(_options),
            _commands,
            _queries,
            []);
    }

    void Because()
    {
        _first = Catch.Exception(() => Touch(_broken));
        _second = Catch.Exception(() => Touch(_broken));
        _noScope = Catch.Exception(() => Touch(_unscoped));
        _control = Catch.Exception(() => Touch(_valid));
    }

    static void Touch(McpToolCatalogFactory factory) => _ = factory.Catalog;

    [Fact] void should_not_provide_a_catalog_when_names_collide() => _first.ShouldBeOfExactType<McpToolNameDuplicated>();
    [Fact] void should_keep_refusing_instead_of_exposing_a_partial_catalog() => _second.ShouldBeOfExactType<McpToolNameDuplicated>();
    [Fact] void should_not_provide_a_catalog_when_no_scope_is_configured() => _noScope.ShouldBeOfExactType<McpToolHasNoScope>();
    [Fact] void should_provide_the_catalog_for_a_valid_configuration() => (_control is null && _valid.Catalog.Tools.Count == 1).ShouldBeTrue();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
