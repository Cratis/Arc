// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_screen_with_routes : a_generated_document
{
    protected const string Imports = """
        import { DataPage, CommandDialog } from '@cratis/components';
        import { useNavigate } from 'react-router-dom';
        import { RetireAuthor } from './RetireAuthor';
        """;

    protected const string RouteImports = """
        import { Route, Routes } from 'react-router-dom';
        import { AuthorDetails as Details } from './Authors/Details/AuthorDetails';
        import { Layout } from './Layout';
        """;

    protected string Component;
    protected string Routes;
    protected string RouteFile = "Library/App.tsx";
    protected (string Path, string Text)[] ExtraFiles = [];

    protected void GenerateNavigation()
    {
        Files = DeclaredUserInterfaceFiles.Holding(
        [
            ("Library/Authors/Retirement/AuthorRetirement.tsx", Component),
            ("Library/Authors/Details/AuthorDetails.tsx", "import { DataPage } from '@cratis/components'; export const AuthorDetails = () => <DataPage title=\"Author details\" />;"),
            (RouteFile, Routes),
            .. ExtraFiles
        ]);
        var retirement = when_generating.LibrarySource.AuthorRetirement.Replace(
            "public static IEnumerable<Author> AuthorsToRetire() => [];",
            "public static IEnumerable<Author> AuthorsToRetire() => []; public static Author? AuthorToRetireById(AuthorId id) => null;",
            StringComparison.Ordinal);
        Generate(
            ("Library/Program.cs", "namespace Library; public class Program {}"),
            ("Library/Authors/Retirement/Retirement.cs", retirement),
            ("Library/Authors/Details/Details.cs", "using Cratis.Arc.Queries.ModelBound; namespace Library.Authors.Details; [ReadModel] public record Detail(string Id) { public static Detail? AuthorDetail(string id) => null; }"));
    }
}
