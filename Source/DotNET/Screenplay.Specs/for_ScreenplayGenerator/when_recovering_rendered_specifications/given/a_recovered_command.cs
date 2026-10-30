// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications.given;

public class a_recovered_command : a_rendered_single_event_command
{
    protected string Name = "DraftStatus";
    protected string Status = "draft";
    protected string Command = "RecordStatus";

    protected void AssertRecovered()
    {
        Result.Source.ShouldContain($"specification {Name}");
        Result.Source.ShouldContain(RenderedEnumCommandSources.Identity);
        Result.Source.ShouldContain($"status = \"{Status}\"");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == "SP0039").ShouldBeEmpty();
        var specification = Result.Model.Slices.SelectMany(slice => slice.Specifications).Single();
        specification.When!.Name.ShouldEqual(Command);
        specification.When.Values.Single(value => value.Property == "Id").Source.ShouldEqual(new LiteralSource(RenderedEnumCommandSources.Identity));
        specification.Then.Single().Name.ShouldEqual("StatusRecorded");
        specification.Then.Single().For.ShouldEqual(new LiteralSource(RenderedEnumCommandSources.Identity));
        specification.Given.ShouldBeEmpty();
        specification.Errors.ShouldBeEmpty();
        specification.Then.Single().Values.Single().Source.ShouldEqual(new LiteralSource(new EnumValue(char.ToUpperInvariant(Status[0]) + Status[1..])));
        var evidence = SpecificationEvidence.For(specification)!;
        evidence.Values.Values.ShouldContain(location => location.SourceTree!.GetText().ToString(location.SourceSpan) == "_id");
        AssertDocument();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", ExpectedEnumCommandDocument.For(Name, Command, Status));
        var expected = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        Assert.True(expected.Success, string.Join(Environment.NewLine, expected.Diagnostics));
        Assert.True(SemanticModelSerializer.Serialize(Bound.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(expected.Value!.Model)), Result.Source);
    }
}
