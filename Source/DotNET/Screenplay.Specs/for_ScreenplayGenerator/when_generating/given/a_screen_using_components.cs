// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_screen_using_components : Specification
{
    protected const string Imports = """
        import { DataPage, MenuItem, MenuItems, CommandDialog, Column } from '@cratis/components';
        import { useState } from 'react';
        import { AuthorsToRetire } from './AuthorsToRetire';
        import { RetireAuthor } from './RetireAuthor';
        """;

    protected string _component;
    protected ScreenplayGenerationResult _result;
    protected CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    protected string _reprinted;

    protected void Generate()
    {
        var sources = new[] { ("Library/Authors/Retirement/Retirement.cs", LibrarySource.AuthorRetirement) };
        var files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Retirement/AuthorRetirement.tsx", _component));
        _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(files), new ScreenplayEmitter())
            .Generate(Analyzed.Compile(sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    protected bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    protected bool Reports(string code, string text) => _result.Diagnostics.Any(_ => _.Code == code && _.Message.Contains(text, StringComparison.Ordinal));

    protected int Count(string code) => _result.Diagnostics.Count(_ => _.Code == code);

    protected bool DocumentIsClean => !_compiled.Diagnostics.WithoutTimelineInformation().Any();
}
