// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>Declaration markers over real Chronicle graph records.</summary>
public static class GraphProjectionDeclarationSources
{
    public const string AllMembers = "typeof(global::Library.ReadModels.Graphs.Person), typeof(global::Library.ReadModels.Graphs.WORKS_AT)";

    public static string Marker(string name = "PeopleDirectory", string members = AllMembers, string location = "Library.People.Directory", string graph = "People", string type = "UnrelatedMarker") => $$"""
        namespace {{location}};
        [global::Cratis.Arc.Slices.GraphProjectionDeclarationAttribute("{{name}}", "{{graph}}", {{members}})]
        public sealed class {{type}} { }
        """;

    public static (string Path, string Text)[] Files(params string[] markers) =>
        [.. GraphSources.Files(), .. markers.Select((marker, index) => ($"Marker{index}.cs", marker))];
}
