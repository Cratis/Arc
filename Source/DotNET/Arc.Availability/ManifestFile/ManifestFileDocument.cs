// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// The availability manifest file: what an application declares about availability, in one format every Arc implementation reads and writes.
/// </summary>
/// <param name="Format">Always <see cref="FormatName"/>.</param>
/// <param name="Version">The version of the format, <see cref="CurrentVersion"/>.</param>
/// <param name="Origin">What wrote it, for example the assembly or the model; empty when unknown.</param>
/// <param name="Audiences">The audiences.</param>
/// <param name="Blocks">The named availability blocks.</param>
/// <param name="Conditions">The conditions that are code and so cannot be evaluated from the file.</param>
/// <param name="ContextValues">The context values a gate can read.</param>
/// <param name="Scopes">The modules, features and slices that carry a gate.</param>
/// <param name="Parts">The commands and read models.</param>
/// <param name="Suspendable">What an operator can suspend.</param>
/// <remarks>
/// The format is described in the availability manifest file documentation of the repository, and its JSON schema is
/// <c>Specifications/availability/manifest.schema.json</c>.
/// </remarks>
public record ManifestFileDocument(
    string Format,
    int Version,
    string Origin,
    IReadOnlyList<ManifestAudience> Audiences,
    IReadOnlyList<ManifestBlock> Blocks,
    IReadOnlyList<ManifestNamedConditionEntry> Conditions,
    IReadOnlyList<ManifestContextValue> ContextValues,
    IReadOnlyList<ManifestScope> Scopes,
    IReadOnlyList<ManifestPart> Parts,
    IReadOnlyList<ManifestSuspendable> Suspendable)
{
    /// <summary>
    /// The value of <see cref="Format"/>.
    /// </summary>
    public const string FormatName = "cratis-availability-manifest";

    /// <summary>
    /// The version of the format this code writes and reads.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// The conventional file name of the manifest.
    /// </summary>
    public const string FileName = "cratis.availability.manifest.json";

    /// <summary>
    /// Gets the options the file is written and read with.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        AllowOutOfOrderMetadataProperties = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets an empty manifest.
    /// </summary>
    public static ManifestFileDocument Empty { get; } = new(FormatName, CurrentVersion, string.Empty, [], [], [], [], [], [], []);

    /// <summary>
    /// Reads a manifest from JSON.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The <see cref="ManifestFileDocument"/>.</returns>
    /// <exception cref="InvalidDataException">The text is not a manifest of a version this code reads.</exception>
    public static ManifestFileDocument Parse(string json)
    {
        ManifestFileDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ManifestFileDocument>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The availability manifest is not valid.", ex);
        }

        if (document is null || document.Format != FormatName)
        {
            throw new InvalidDataException($"The file is not an availability manifest: its format is not '{FormatName}'.");
        }

        if (document.Version != CurrentVersion)
        {
            throw new InvalidDataException($"The availability manifest is version {document.Version}; this reads version {CurrentVersion}.");
        }

        return document;
    }

    /// <summary>
    /// Writes the manifest as JSON.
    /// </summary>
    /// <returns>The JSON text.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}
