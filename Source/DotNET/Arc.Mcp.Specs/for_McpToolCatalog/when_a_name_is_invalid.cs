// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_a_name_is_invalid : a_catalog_context
{
    static readonly string[] _invalidNames = ["Upper", "1starts_with_digit", "has-dash", string.Empty, " ", "has space", "path/ish", "..", new string('a', 65), "_leading"];
    static readonly string _longestValidName = new('a', 64);

    List<Exception?> _errors;
    Exception? _control;

    void Because()
    {
        _errors = [.. _invalidNames.Select(name => Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<DeclaredCommand>(name, "Described."), _options, _commands, _queries)))];
        _control = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<DeclaredCommand>(_longestValidName, "Described."), _options, _commands, _queries));
    }

    [Fact] void should_refuse_every_invalid_name() => _errors.TrueForAll(_ => _ is McpToolNameInvalid).ShouldBeTrue();
    [Fact] void should_have_checked_every_name() => _errors.Count.ShouldEqual(_invalidNames.Length);
    [Fact] void should_accept_a_valid_name_of_the_longest_length() => _control.ShouldBeNull();
}
