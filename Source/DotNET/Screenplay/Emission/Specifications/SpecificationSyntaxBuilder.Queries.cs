// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

public partial class SpecificationSyntaxBuilder
{
    /// <summary>
    /// Builds the query a scenario calls and the read model it expects back.
    /// </summary>
    /// <param name="read">The query read, or <see langword="null"/> when the scenario is not a query scenario.</param>
    /// <returns>The <c>then query</c> assertions - one for a query scenario, none otherwise.</returns>
    /// <remarks>
    /// The expected read model is stated as a subset, never <c>exactly</c>: a whole-result comparison in the source says
    /// every value it states must match, which is what a subset of every declared property already says.
    /// </remarks>
    IEnumerable<SpecificationQuerySyntax> Queries(SpecificationQueryModel? read) =>
        read is null
            ? []
            :
            [
                new SpecificationQuerySyntax(
                    naming.ToDeclarationName(read.Query),
                    [.. Mappings(read.Arguments)],
                    [new SpecificationQueryResultSyntax([.. Mappings(read.Result)], SourceLocation.Start)],
                    SourceLocation.Start)
            ];

    IEnumerable<PropertyMappingSyntax> Mappings(IEnumerable<PropertyMappingModel> values) =>
        values.Select(value => new PropertyMappingSyntax(naming.ToPropertyName(value.Property), FixtureValue(value.Source), SourceLocation.Start));
}
