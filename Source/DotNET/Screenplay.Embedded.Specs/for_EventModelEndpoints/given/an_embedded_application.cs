// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

/// <summary>
/// Builds the assemblies the specifications serve documents out of.
/// </summary>
/// <remarks>
/// The runtime reads documents out of manifest resources, so a specification needs an assembly embedding
/// them. Emitting one in memory keeps the fixtures beside the specifications that use them, and lets a
/// malformed catalog be specified without a project that will not build.
/// </remarks>
[SuppressMessage("Usage", "MA0136:Raw String contains an implicit end of line character", Justification = "The authored fixture literals are explicitly normalized to LF before compilation.")]
public static class an_embedded_application
{
    public const string ProjectId = "Fixture.Application";
    public const string ApplicationDocumentId = "Fixture.Application";
    public const string ModuleDocumentId = "Fixture.Application.Catalog";
    public const string ApplicationResourceName = "Fixture.Application.play";
    public const string ModuleResourceName = "Fixture.Application.Catalog.play";

    public static readonly string ApplicationSource = """
        domain Fixture

        module Catalog
          description "Books in the catalog"

          feature Books
            description "Registering and listing books"

            slice StateChange RegisterBook
              description "Registers a book"

              command RegisterBook
                description "Registers a book in the catalog"
                bookId Uuid identifier
                title  String
                validate
                  title not empty message "Title is required"

              event BookRegistered
                bookId Uuid
                title  String

              screen RegisterBookScreen

            slice StateView BookList
              description "Lists the books in the catalog"

              query ListBooks => BookListReadModel[]
                description "Every book in the catalog"
                filter title String?

              readmodel BookListReadModel
                id    Uuid
                title String

              projection BookList => BookListReadModel
                from BookRegistered key bookId
                  id    = $eventSourceId
                  title = title

        """.ReplaceLineEndings("\n");

    public static readonly string ModuleSource = """
        module Catalog
          description "Books in the catalog"

          feature Books
            slice StateChange RegisterBook
              command RegisterBook
                bookId Uuid identifier
                title  String

              event BookRegistered
                bookId Uuid
                title  String

        """.ReplaceLineEndings("\n");

    static readonly string _catalog = $$"""
        {
            "documents": [
                {
                    "id": "{{ApplicationDocumentId}}",
                    "title": "Fixture Application",
                    "namespace": "{{ApplicationDocumentId}}",
                    "kind": "assembly",
                    "parentId": null,
                    "resourceName": "{{ApplicationResourceName}}"
                },
                {
                    "id": "{{ModuleDocumentId}}",
                    "title": "Catalog",
                    "namespace": "{{ModuleDocumentId}}",
                    "kind": "module",
                    "parentId": "{{ApplicationDocumentId}}",
                    "resourceName": "{{ModuleResourceName}}"
                }
            ]
        }
        """.ReplaceLineEndings("\n");

    static readonly Lazy<Assembly> _assembly = new(() => Emit(
        ProjectId,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EventModelCatalog.ResourceName] = _catalog,
            [ApplicationResourceName] = ApplicationSource,
            [ModuleResourceName] = ModuleSource
        }));

    /// <summary>
    /// Gets an assembly embedding a catalog naming two documents.
    /// </summary>
    public static Assembly Assembly => _assembly.Value;

    /// <summary>
    /// Gets an assembly embedding a catalog that cannot be read as written.
    /// </summary>
    /// <param name="name">The name to give the assembly, so each specification emits its own.</param>
    /// <param name="catalog">The catalog to embed.</param>
    /// <returns>The resulting assembly.</returns>
    public static Assembly WithCatalog(string name, string catalog) =>
        Emit(name, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EventModelCatalog.ResourceName] = catalog,
            [ApplicationResourceName] = ApplicationSource
        });

    /// <summary>
    /// Gets an assembly embedding no catalog at all.
    /// </summary>
    /// <param name="name">The name to give the assembly.</param>
    /// <returns>The resulting assembly.</returns>
    public static Assembly WithoutCatalog(string name) =>
        Emit(name, new Dictionary<string, string>(StringComparer.Ordinal));

    static Assembly Emit(string name, IDictionary<string, string> resources)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(string.Empty)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(
            stream,
            manifestResources:
            [
                .. resources.Select(resource => new ResourceDescription(
                    resource.Key,
                    () => new MemoryStream(Encoding.UTF8.GetBytes(resource.Value)),
                    isPublic: true))
            ]);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"The fixture assembly '{name}' could not be emitted - {string.Join(", ", result.Diagnostics.Select(diagnostic => diagnostic.ToString()))}.");
        }

        return Assembly.Load(stream.ToArray());
    }
}
