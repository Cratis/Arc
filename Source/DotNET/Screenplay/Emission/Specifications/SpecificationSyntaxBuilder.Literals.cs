// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

public partial class SpecificationSyntaxBuilder
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.None, 1000)]
    private static partial Regex IsoInstant();

    static bool Fits(string primitive, object? value, ConceptModel? concept) => primitive switch
    {
        "String" => value is string,
        "Int" => value is int or long,
        "Decimal" => value is int or long or float or double or decimal,
        "Bool" => value is bool,
        _ => FitsStructured(primitive, value, concept)
    };

    static bool FitsStructured(string primitive, object? value, ConceptModel? concept) => primitive switch
    {
        "Uuid" => value is string uuid && Guid.TryParse(uuid, out var parsed) && parsed.ToString("D", CultureInfo.InvariantCulture) == uuid,
        "Date" => value is string date && DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        "DateTime" => value is string instant && IsoInstant().IsMatch(instant) && DateTimeOffset.TryParse(instant, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        "Enum" => value is string member && concept!.EnumValues.Contains(member, StringComparer.Ordinal),
        _ => false
    };

    bool CanStateStreamId(LiteralSource id, TypeReferenceModel type) => CanStateLiteral(id, type) &&
            (id.Value is not string text || (text.Length > 0 && text.IsNormalized() && naming.ToStringLiteral(text) == text)) &&
            (id.Value is not long number || number is >= -9007199254740991 and <= 9007199254740991);

    bool CanStateLiteral(LiteralSource literal, TypeReferenceModel type)
    {
        if (type.IsOptional || type.IsCollection)
        {
            return false;
        }

        var name = naming.ToDeclarationName(type.Name);
        var concept = Application?.Concepts.SingleOrDefault(concept => naming.ToDeclarationName(concept.Name) == name);

        return Fits(concept?.Primitive.ToString() ?? name, literal.Value, concept);
    }
}
