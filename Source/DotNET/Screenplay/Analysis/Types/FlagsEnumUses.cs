// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Arc.Screenplay.Emission.Naming;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Tracks flags enumerations whose unnamed combinations cannot be stated in the document.
/// </summary>
internal class FlagsEnumUses
{
    readonly Dictionary<string, (ITypeSymbol Type, HashSet<string> Uses)> _enumerations = new(StringComparer.Ordinal);

    /// <summary>
    /// Records the enumeration carried directly or through a concept by a property.
    /// </summary>
    /// <param name="property">The analyzed property.</param>
    public void Record(IPropertySymbol property)
    {
        var carried = UnderlyingTypes.Of(property.Type);
        var type = carried.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments[0] ?? carried;
        if (type.TypeKind != TypeKind.Enum || !type.HasAttribute("System.FlagsAttribute"))
        {
            return;
        }

        var name = type.ToDisplayString();
        if (!_enumerations.TryGetValue(name, out var entry))
        {
            entry = (type, new(StringComparer.Ordinal));
            _enumerations[name] = entry;
        }

        entry.Uses.Add($"{property.ContainingType.ToDisplayString()}.{property.Name}");
    }

    /// <summary>
    /// Reports once per enumeration when its positive members are not closed under bitwise OR.
    /// </summary>
    /// <param name="diagnostics">Where the limitations are reported.</param>
    public void Report(ScreenplayDiagnostics diagnostics)
    {
        foreach (var entry in _enumerations.Values.OrderBy(entry => entry.Type.ToDisplayString(), StringComparer.Ordinal))
        {
            var members = PositiveMembers(entry.Type);
            var numbers = members.Select(member => member.Number).ToHashSet();
            if (numbers.All(left => numbers.All(right => numbers.Contains(left | right))))
            {
                continue;
            }

            var combinations = members.Where(member => (member.Number & (member.Number - 1)) != 0)
                .Select(member => new ScreenplayNaming().ToPropertyName(member.Name)).Order(StringComparer.Ordinal).ToList();
            var named = combinations.Count == 0 ? string.Empty : $"; named combinations {string.Join(", ", combinations.Select(name => $"'{name}'"))} are declared as ordinary members";
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.FlagsCombinationsNotStatable,
                $"Enum '{entry.Type.Name}' is [Flags]; {string.Join(", ", entry.Uses.Order(StringComparer.Ordinal).Select(use => $"'{use}'"))} can hold combinations, but the document declares one member at a time, so combinations no member names cannot be stated (Screenplay#319){named}",
                entry.Type.ToDisplayString());
        }
    }

    static IReadOnlyList<(string Name, BigInteger Number)> PositiveMembers(ITypeSymbol type) =>
        [.. type.GetMembers().OfType<IFieldSymbol>().Where(member => member.HasConstantValue && member.Name == EnumConstants.MemberOf(type, member.ConstantValue!))
            .Select(member => (member.Name, Number: EnumConstants.Normalize(member.ConstantValue)!.Value))
            .Where(member => member.Number > 0)];
}
