// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when the JSON serializer options are configured.
/// </summary>
public class when_binding_json_serializer_options : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:JsonSerializerOptions:WriteIndented"] = "true",
        ["Cratis:Arc:JsonSerializerOptions:MaxDepth"] = "12",
        ["Cratis:Arc:JsonSerializerOptions:NumberHandling"] = "AllowReadingFromString, WriteAsString",
        ["Cratis:Arc:JsonSerializerOptions:DefaultIgnoreCondition"] = "WhenWritingNull",
        ["Cratis:Arc:JsonSerializerOptions:IndentCharacter"] = "\t",
        ["Cratis:Arc:JsonSerializerOptions:IndentSize"] = "2",
        ["Cratis:Arc:JsonSerializerOptions:NewLine"] = "\n",
        ["Cratis:Arc:JsonSerializerOptions:IsReadOnly"] = "true",
        ["Cratis:Arc:JsonSerializerOptions:Converters:0"] = "ignored"
    };
}
