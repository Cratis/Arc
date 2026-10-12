// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_EmittedIdentityCompliance.when_applying;

public class with_a_reaction_destination : given.a_document
{
    [Theory]
    [InlineData("event")]
    [InlineData("clause")]
    [InlineData("declared trigger")]
    public void should_resolve_the_protected_trigger_property(string shape)
    {
        var module = Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var name = shape == "event" ? "Saved" : "External";
        var data = new TriggerDataSyntax("value", new TypeRefSyntax("ProtectedValue", false, false, SourceLocation.Start), SourceLocation.Start);
        var produced = new ProducesSyntax("FollowedUp", null, [new PropertyMappingSyntax("value", new PathExpressionSyntax("value", SourceLocation.Start), SourceLocation.Start)], SourceLocation.Start)
        {
            For = new PathExpressionSyntax("value", SourceLocation.Start)
        };
        var trigger = new ReactionTriggerSyntax(new NamedTriggerSourceSyntax(name, SourceLocation.Start), shape == "clause" ? [data] : [], null, null, SourceLocation.Start, Produces: [produced]);
        Application = Application with
        {
            Imports = shape == "clause" ? [new ImportSyntax("External", SourceLocation.Start)] : [],
            Triggers = shape == "declared trigger" ? [new TriggerSyntax("External", [data], SourceLocation.Start)] : [],
            Modules = [module with { Features = [feature with { Slices = [slice with
            {
                Events = slice.Events.Append(slice.Events.Single() with { Name = "FollowedUp" }).ToList(),
                Reactions = [new ReactionSyntax("FollowUp", [trigger], SourceLocation.Start)]
            }] }] }]
        };

        Apply();

        AssertStripped($"for destination of 'FollowedUp' in reaction 'FollowUp' on '{name}'");
    }
}
