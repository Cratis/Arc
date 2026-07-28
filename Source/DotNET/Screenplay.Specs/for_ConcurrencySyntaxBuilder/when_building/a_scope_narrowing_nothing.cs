// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConcurrencySyntaxBuilder.when_building;

/// <summary>
/// A concurrency block that narrows nothing at all is a compile error, so it has to be left out rather than emitted
/// empty - and left out visibly, since a command that silently loses its concurrency scope reads as if it never had
/// one.
/// </summary>
public class a_scope_narrowing_nothing : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ConcurrencySyntaxBuilder _builder;
    ConcurrencySyntax? _result;

    void Establish()
    {
        _diagnostics = new();
        _builder = new(new ScreenplayNaming(), _diagnostics);
    }

    void Because() => _result = _builder.Build(new ConcurrencyModel(false, null, null, null, []), "Library.Inventory.Adding");

    [Fact] void should_emit_no_block() => _result.ShouldBeNull();
    [Fact] void should_report_the_scope() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.EmptyConcurrencyScope]);
}
