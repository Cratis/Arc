// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

public partial class SpecificationSyntaxBuilder
{
    SpecificationModel? Prepare(SpecificationModel specification)
    {
        var caller = WithRepresentableCaller(specification);
        var sources = caller is null ? null : WithRepresentableSources(caller);
        var reactions = sources is null ? null : WithReactionsAccountedFor(sources);

        return reactions is null ? null : WithFollowingAssertions(reactions);
    }

    IEnumerable<SpecificationModel> FoldCases(IReadOnlyList<(SpecificationModel Original, SpecificationModel? Prepared)> rows)
    {
        var folded = new HashSet<SpecificationCaseTable>();
        foreach (var row in rows.Where(row => row.Prepared is not null))
        {
            var table = SpecificationCaseTable.For(row.Original);
            if (table is null)
            {
                yield return row.Prepared!;
                continue;
            }

            if (CanFold(table, rows))
            {
                if (folded.Add(table))
                {
                    yield return table.Symbolic;
                }
            }
            else
            {
                Diagnostics?.Information(
                    ScreenplayDiagnosticCodes.SpecificationCasesNotFolded,
                    $"The scenario '{row.Original.Name}' stays per row because emission changes or omits a row, or the table's parameter or expanded case names would collide",
                    row.Original.Name);
                yield return row.Prepared!;
            }
        }
    }

    bool CanFold(SpecificationCaseTable table, IReadOnlyList<(SpecificationModel Original, SpecificationModel? Prepared)> rows)
    {
        var members = rows.Where(row => ReferenceEquals(SpecificationCaseTable.For(row.Original), table)).ToList();
        if (table.Parameters.Count == 0 || table.Rows.Select(row => row.Name).Distinct(StringComparer.Ordinal).Count() != table.Rows.Count ||
            table.Parameters.Select(parameter => naming.ToPropertyName(parameter.Name)).Distinct(StringComparer.Ordinal).Count() != table.Parameters.Count ||
            members.Count != table.Rows.Count || members.Exists(row => row.Prepared is null || !SpecificationCaseComparison.Matches(row.Original, row.Prepared)))
        {
            return false;
        }

        var names = rows.Where(row => row.Prepared is not null && !ReferenceEquals(SpecificationCaseTable.For(row.Original), table))
            .Select(row => naming.ToDeclarationName(row.Prepared!.Name)).ToHashSet(StringComparer.Ordinal);
        var name = naming.ToDeclarationName(table.Symbolic.Name);

        return !names.Contains(name) && table.Rows.All(row => !names.Contains($"{name}_{row.Name}"));
    }

    List<SpecificationParameterSyntax> CaseParameters(SpecificationModel specification) =>
        SpecificationCaseTable.For(specification) is { } table && ReferenceEquals(table.Symbolic, specification)
            ? table.Parameters.Select(parameter => new SpecificationParameterSyntax(naming.ToPropertyName(parameter.Name), new TypeReferenceConverter(naming).Convert(parameter.Type), SourceLocation.Start)).ToList()
            : [];

    List<SpecificationCaseSyntax> CaseRows(SpecificationModel specification) =>
        SpecificationCaseTable.For(specification) is { } table && ReferenceEquals(table.Symbolic, specification)
            ? table.Rows.Select(row => new SpecificationCaseSyntax(
                row.Name,
                row.Values.Select(value => new PropertyMappingSyntax(naming.ToPropertyName(value.Property), _sources.Convert(value.Source), SourceLocation.Start)).ToList(),
                SourceLocation.Start)).ToList()
            : [];

    ExpressionSyntax CaseValue(MappingSourceModel source) => source is PropertyPathSource path && path.Path.StartsWith("case.", StringComparison.Ordinal)
        ? new CaseValueExpressionSyntax(naming.ToPropertyName(path.Path[5..]), SourceLocation.Start)
        : _sources.Convert(source);
}
