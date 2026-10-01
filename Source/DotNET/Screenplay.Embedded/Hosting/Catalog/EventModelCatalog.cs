// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents the Screenplay documents embedded in a set of assemblies.
/// </summary>
/// <remarks>
/// The catalog is read once, when the explorer is mapped, so a package whose catalog disagrees with what it
/// embeds fails at startup rather than on the first request that happens to need the missing piece.
/// </remarks>
public sealed class EventModelCatalog
{
    /// <summary>
    /// The logical name the catalog document is embedded under.
    /// </summary>
    public const string ResourceName = "Cratis.Arc.Screenplay.Embedded.catalog.json";

    static readonly JsonSerializerOptions _manifestSerializerOptions = new(JsonSerializerDefaults.Web);

    readonly FrozenDictionary<string, CatalogProject> _projects;

    EventModelCatalog(IReadOnlyList<CatalogProject> projects)
    {
        _projects = projects.ToFrozenDictionary(project => project.Project.Id, StringComparer.Ordinal);
        Projects = [.. projects.Select(project => project.Project)];
    }

    /// <summary>
    /// Gets the projects - one per assembly embedding a catalog - and the documents they hold.
    /// </summary>
    public IReadOnlyList<EventModelProject> Projects { get; }

    /// <summary>
    /// Reads the catalog embedded in the given assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to read embedded catalogs from.</param>
    /// <returns>The resulting <see cref="EventModelCatalog"/>.</returns>
    /// <exception cref="MalformedEventModelCatalog">Thrown when an assembly embeds a catalog that cannot be read as written.</exception>
    /// <remarks>
    /// An assembly without a catalog contributes nothing and is left out; an assembly with a catalog that cannot
    /// be read is an error, since the documents it promises would otherwise go missing without anyone saying so.
    /// </remarks>
    public static EventModelCatalog For(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var projects = new List<CatalogProject>();
        foreach (var assembly in assemblies.Distinct())
        {
            if (Read(assembly) is { } project)
            {
                projects.Add(project);
            }
        }

        return new EventModelCatalog(projects);
    }

    /// <summary>
    /// Tries to get the <c>.play</c> source of a document.
    /// </summary>
    /// <param name="projectId">The identifier of the project holding the document.</param>
    /// <param name="documentId">The identifier of the document.</param>
    /// <param name="source">When this method returns, holds the source of the document - or an empty string when it is unknown.</param>
    /// <returns>True when the document is known and its source could be read, false otherwise.</returns>
    /// <exception cref="MalformedEventModelCatalog">Thrown when the resource the catalog names cannot be read.</exception>
    public bool TryGetSource(string projectId, string documentId, out string source)
    {
        source = string.Empty;
        if (!_projects.TryGetValue(projectId, out var project) || !project.Documents.TryGetValue(documentId, out var document))
        {
            return false;
        }

        source = ReadResource(project.Assembly, document.ResourceName);
        return true;
    }

    static CatalogProject? Read(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? assembly.FullName ?? assembly.ToString();
        if (assembly.GetManifestResourceStream(ResourceName) is not { } stream)
        {
            return null;
        }

        EventModelCatalogManifest? manifest;
        using (stream)
        {
            try
            {
                manifest = JsonSerializer.Deserialize<EventModelCatalogManifest>(stream, _manifestSerializerOptions);
            }
            catch (JsonException ex)
            {
                throw new MalformedEventModelCatalog(assemblyName, "it is not valid JSON", ex);
            }
        }

        if (manifest?.Documents is null)
        {
            throw new MalformedEventModelCatalog(assemblyName, "it does not hold a 'documents' collection");
        }

        var resourceNames = assembly.GetManifestResourceNames().ToHashSet(StringComparer.Ordinal);
        var documents = manifest.Documents.Select(document => ToDocument(assemblyName, document, resourceNames)).ToList();
        var duplicate = documents.GroupBy(document => document.Id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new MalformedEventModelCatalog(assemblyName, $"it holds more than one document with the identifier '{duplicate.Key}'");
        }

        var known = documents.Select(document => document.Id).ToHashSet(StringComparer.Ordinal);
        var orphan = documents.Find(document => document.ParentId is not null && !known.Contains(document.ParentId));
        if (orphan is not null)
        {
            throw new MalformedEventModelCatalog(assemblyName, $"the document '{orphan.Id}' names the parent '{orphan.ParentId}', which it does not hold");
        }

        return new CatalogProject(
            assembly,
            new EventModelProject(assemblyName, assemblyName, documents),
            documents.ToFrozenDictionary(document => document.Id, StringComparer.Ordinal));
    }

    static EventModelDocument ToDocument(string assemblyName, EventModelCatalogManifestDocument document, HashSet<string> resourceNames)
    {
        if (string.IsNullOrWhiteSpace(document.Id))
        {
            throw new MalformedEventModelCatalog(assemblyName, "it holds a document without an identifier");
        }

        if (string.IsNullOrWhiteSpace(document.ResourceName))
        {
            throw new MalformedEventModelCatalog(assemblyName, $"the document '{document.Id}' does not name the resource holding its source");
        }

        if (!resourceNames.Contains(document.ResourceName))
        {
            throw new MalformedEventModelCatalog(assemblyName, $"the document '{document.Id}' names the resource '{document.ResourceName}', which the assembly does not embed");
        }

        if (!Enum.TryParse<EventModelDocumentKind>(document.Kind, true, out var kind) || !Enum.IsDefined(kind))
        {
            throw new MalformedEventModelCatalog(assemblyName, $"the document '{document.Id}' declares the kind '{document.Kind}', which is not one of assembly, module or feature");
        }

        return new EventModelDocument(
            document.Id,
            string.IsNullOrWhiteSpace(document.Title) ? document.Id : document.Title,
            document.Namespace ?? document.Id,
            kind,
            string.IsNullOrWhiteSpace(document.ParentId) ? null : document.ParentId,
            document.ResourceName);
    }

    static string ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName) ??
            throw new MalformedEventModelCatalog(
                assembly.GetName().Name ?? assembly.ToString(),
                $"the resource '{resourceName}' it names is no longer embedded");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    record CatalogProject(Assembly Assembly, EventModelProject Project, FrozenDictionary<string, EventModelDocument> Documents);
}
