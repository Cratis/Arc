// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_nested_event_returns : a_generated_document
{
    [Theory]
    [InlineData("AuthorRegistered Registered() { return new AuthorRegistered(Name); }")]
    [InlineData("Func<AuthorRegistered> Registered = () => { return new AuthorRegistered(Name); };")]
    public void should_not_retarget_an_explicit_append_from_a_nested_return(string nested)
    {
        var source = IdentifierSources.With("""
            [Command] public record RegisterAuthor([Key] Guid Id, Guid OtherId, string Name)
            {
                public async System.Threading.Tasks.Task Handle(Cratis.Chronicle.EventSequences.IEventLog eventLog)
                {
            """ + nested + """
                    await eventLog.Append(OtherId, Registered());
                }
            }
            """);
        Analyzed.ErrorsIn(Analyzed.Compile((Analyzed.SlicePath, source))).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));

        Result.Source.ShouldNotContain("for id");
        Result.Source.ShouldNotContain(" identifier");
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().UsesCommandContext.ShouldBeFalse();
        AssertDocument();
    }

    [Theory]
    [InlineData("(AuthorId, AuthorRegistered) Registered() { return (id, new AuthorRegistered(Name)); }")]
    [InlineData("Func<(AuthorId, AuthorRegistered)> Registered = () => { return (id, new AuthorRegistered(Name)); };")]
    public void should_not_treat_a_nested_generated_identity_tuple_as_a_handler_return(string nested)
    {
        var source = IdentifierSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public void Handle()
                {
                    AuthorId id = new(Guid.NewGuid());
            """ + nested + "Registered(); } }");
        var compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == Analyzed.SlicePath);
        var model = compilation.GetSemanticModel(tree);
        var body = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Handle").Body!;
        var creation = body.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().Single(node => model.GetTypeInfo(node).Type?.Name == "AuthorRegistered");

        ProductionDestinations.ThroughCommandContext(creation, body, model, aggregate: false, generatedIdentity: true).ShouldBeFalse();
    }
}
