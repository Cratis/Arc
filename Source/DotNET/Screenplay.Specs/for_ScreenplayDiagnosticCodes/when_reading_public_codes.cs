// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Screenplay.for_ScreenplayDiagnosticCodes;

public class when_reading_public_codes : Specification
{
    [Fact] void should_preserve_the_published_unrecoverable_graph_code() => ScreenplayDiagnosticCodes.UnrecoverableGraph.ShouldEqual("SP0082");
    [Fact] void should_preserve_the_published_canonical_graph_placement_code() => ScreenplayDiagnosticCodes.CanonicalGraphPlacement.ShouldEqual("SP0083");
    [Fact] void should_preserve_the_published_graph_version_code() => ScreenplayDiagnosticCodes.GraphRequiresV38.ShouldEqual("SP0084");
    [Fact] void should_allocate_the_unpublished_anonymous_authorization_code() => ScreenplayDiagnosticCodes.AnonymousAuthorizationNotRepresentable.ShouldEqual("SP0085");
    [Fact] void should_allocate_the_unpublished_anonymous_registration_code() => ScreenplayDiagnosticCodes.AmbiguousAnonymousPolicyRegistration.ShouldEqual("SP0086");

    [Fact]
    void should_assign_a_unique_value_to_every_public_string_constant()
    {
        var codes = typeof(ScreenplayDiagnosticCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();
        codes.ShouldNotBeEmpty();
        codes.Distinct(StringComparer.Ordinal).Count().ShouldEqual(codes.Length);
    }
}
