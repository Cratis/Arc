// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_SpecificationSyntaxBuilder.when_building;

public class an_append_with_a_read_model_assertion : Specification
{
    string _source;
    SemanticSpecificationRun _run;

    void Because()
    {
        var appended = new SpecificationStateModel("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("Jane Austen"))]);
        var readModel = new SpecificationStateModel("Author", SpecificationStateKind.ReadModel, [new("Id", new LiteralSource("author")), new("Name", new LiteralSource("Prior"))]);
        var specification = new SpecificationModel("Appending", [readModel], appended, [appended, readModel], []);
        var syntax = new SpecificationSyntaxBuilder(new ScreenplayNaming()).Build([specification]).Single();
        var printed = new ScreenplayPrinter().Print(syntax);
        _source = """
            domain Library
            module Library
              feature Authors
                slice StateChange Registration
                  command RegisterAuthor
                    id String identifier
                    name String
                    produces AuthorRegistered
                      for id
                      name = name
                    returns name
                  event AuthorRegistered
                    name String
                  readmodel Author
                    id String
                    name String
                  query AuthorById => Author optional
                    by id String
            """ + "\n" + string.Join('\n', printed.Split('\n').Select(line => "      " + line));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string Key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", _source);
        var bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(Environment.NewLine, bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        var plan = SemanticExecutionPlan.Compile(bound.Value.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    [Fact] void should_keep_the_append() => _source.ShouldContain("when append AuthorRegistered");
    [Fact] void should_drop_the_restatement() => _source.ShouldNotContain("then AuthorRegistered");
    [Fact] void should_keep_the_read_model_assertion() => _source.ShouldContain("then readmodel Author");
    [Fact] void should_pass_reference_execution() => Assert.True(_run.Passed, string.Join(Environment.NewLine, _run.Failures));

    [Theory]
    [InlineData("OtherEvent", "Jane Austen", "current")]
    [InlineData("AuthorRegistered", "Other", "current")]
    [InlineData("AuthorRegistered", "Jane Austen", "other")]
    public void should_not_drop_a_different_fact(string name, string value, string source)
    {
        var action = new SpecificationStateModel("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("Jane Austen"))]) { For = new("current") };
        var expected = new SpecificationStateModel(name, SpecificationStateKind.Event, [new("Name", new LiteralSource(value))]) { For = new(source) };
        var specification = new SpecificationModel("Appending", [], action, [expected], []);
        var command = new CommandModel("RegisterAuthor", null, [new("Id", new("String", false, false))], null, [], [new("AuthorRegistered", null, []) { UsesCommandContext = true }], null, null) { Identifier = "Id" };
        var slice = SliceModel.Empty("Library.Authors.Registration", "Registration", SliceKind.StateChange) with { Commands = [command] };
        var application = new ApplicationModel("Library", "Library", [], [], [slice], []);
        var syntax = new SpecificationSyntaxBuilder(new ScreenplayNaming()) { Application = application }.Build([specification]).Single();
        new ScreenplayPrinter().Print(syntax).ShouldContain($"then {name}");
    }
}
