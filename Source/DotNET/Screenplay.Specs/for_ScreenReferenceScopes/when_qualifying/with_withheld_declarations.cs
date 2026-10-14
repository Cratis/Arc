// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenReferenceScopes.when_qualifying;

public class with_withheld_declarations : given.an_application_with_screen_references
{
    void Establish() => Model = Model with
    {
        Slices = Model.Slices.Select(slice => slice.Name == "Registration" ? slice with { Commands = [] } : slice).ToList()
    };

    void Because() => Emit();

    [Fact] void should_report_each_withheld_reference() => Emission.Diagnostics.Count(_ => _.Code == "SP0028").ShouldEqual(3);
    [Fact] void should_not_emit_unresolved_actions() => Emission.Source.Contains("action ", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_emit_unresolved_query_bindings() => Emission.Source.Contains("via query ", StringComparison.Ordinal).ShouldBeFalse();
}
