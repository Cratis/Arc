// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Printing;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// Query descriptions, parameter defaults, and implementation references survive generation without invented context sources.
/// </summary>
public class with_query_metadata : Specification
{
    const string Source = """
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries;
        using Cratis.Arc.Queries.ModelBound;
        namespace Library.Authors.Listing;
        [ReadModel]
        public record Author(Guid Id, string Name)
        {
            /// <summary>One author, if present.</summary>
            public static Task<Author?> ById(Guid id) => Task.FromResult<Author?>(null);
            public static IEnumerable<Author> All(int limit = 10, string? name = null) { return []; }
            public static Task<Author[]?> MaybeAll() => Task.FromResult<Author[]?>(null);
            public static Author? WithContext(QueryContext context, Guid id) => null;
            public static extern Author? WithoutBody(Guid id);
        }
        """;

    ScreenplayGenerationResult _default;
    ScreenplayGenerationResult _authoring;

    void Because()
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, Source));
        _default = new ScreenplayGenerator().Generate(compilation, new());
        _authoring = new ScreenplayGenerator().Generate(compilation, new() { AuthoringOnlyConstructs = true });
    }

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_recover_the_summary() => _default.Source.ShouldContain("description \"One author, if present.\"");
    [Fact] void should_preserve_a_nullable_wrapped_scalar_return() => _default.Source.ShouldContain("query ById => Author optional");
    [Fact] void should_preserve_a_nullable_collection_return() => _default.Source.ShouldContain("query MaybeAll => Author[] optional");
    [Fact] void should_preserve_non_nullable_collection_returns() => _default.Source.ShouldContain("query All => Author[]\n");
    [Fact] void should_recover_value_parameter_defaults_as_optional() => _default.Source.ShouldContain("filter limit Int optional");
    [Fact] void should_recover_null_defaults_as_optional() => _default.Source.ShouldContain("filter name String optional");
    [Fact] void should_report_the_unrepresented_default_value() => _default.Diagnostics.Any(d => d.Code == "SP0019" && d.Message.Contains("non-null default value", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_fabricate_scalar_context_sources() => _default.Source.ShouldNotContain("from $context");
    [Fact] void should_report_the_context_source_limit() => _default.Diagnostics.Any(d => d.Code == "SP0041" && d.Message.Contains("no 'from $context' source was inferred", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_withhold_performers_by_default() => _default.Source.ShouldNotContain("performer");
    [Fact] void should_report_withheld_performers() => _default.Diagnostics.Count(d => d.Code == "SP0019" && d.Message.Contains("performer references are authoring-only", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_recover_repository_relative_performers_when_requested() => _authoring.Source.Split('\n').Count(line => line.Trim() == "file Feature/Slice/Slice.cs").ShouldEqual(5);
    [Fact] void should_include_each_body_as_a_performer() => _authoring.Source.Split('\n').Count(line => line.Trim() == "performer").ShouldEqual(4);
    [Fact] void should_compile_default_output_without_findings() => new ScreenplayCompiler().Compile(_default.Source).Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_authoring_output_without_findings() => new ScreenplayCompiler().Compile(_authoring.Source).Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_without_unexpected_errors() => _default.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    [Fact] void should_bind_authoring_without_unexpected_errors() => _authoring.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    [Fact]
    void should_report_bodies_without_a_portable_source_path()
    {
        var result = new ScreenplayGenerator().Generate(Analyzed.Compile((string.Empty, Source)), new() { AuthoringOnlyConstructs = true });
        result.Diagnostics.Count(d => d.Code == "SP0019" && d.Message.Contains("no portable performer file path", StringComparison.Ordinal)).ShouldEqual(4);
        result.Source.ShouldNotContain("performer");
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    }

    [Fact] void should_round_trip_authoring_output() => new ScreenplayPrinter().Print(new ScreenplayCompiler().Compile(_authoring.Source).Value!).ShouldEqual(_authoring.Source);
}
