// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_cross_slice_screen : a_generated_document
{
    internal const string Listing = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        namespace Library.Authors.Listing;
        [ReadModel]
        public record Author(string AuthorId, string Name)
        {
            public static IEnumerable<Author> All() => [];
        }
        """;

    internal const string Registration = """
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Authors.Registration;
        [EventType]
        public record AuthorRegistered(string Name);
        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        [ReadModel]
        public record RegisteredAuthor(string RegisteredAuthorId, string Name)
        {
            public static IEnumerable<RegisteredAuthor> RegisteredAuthors() => [];
        }
        """;

    protected string Component;
    protected (string Path, string Text)[] AdditionalSources = [];
    protected ScreenplayOptions Options = new();

    protected void GenerateScreen()
    {
        Files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Listing/AuthorBoard.tsx", Component));
        Generate(Options,
            [("Library/Authors/Listing/Listing.cs", Listing), ("Library/Authors/Registration/Registration.cs", Registration), .. AdditionalSources]);
    }

    protected bool Says(string text) => Result.Source.Contains(text, StringComparison.Ordinal);
}
