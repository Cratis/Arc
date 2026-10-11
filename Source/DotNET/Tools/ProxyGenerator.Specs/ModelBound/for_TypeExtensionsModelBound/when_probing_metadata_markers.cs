// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Cratis.Arc.ProxyGenerator.Specs.ActualDependency;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

#pragma warning disable ARC0026 // Intentionally invalid owner used to test metadata validation.

public class when_probing_metadata_markers : Specification
{
    MetadataLoadContext _context;
    Assembly _assembly;

    void Establish()
    {
        var file = typeof(when_probing_metadata_markers).Assembly.Location;
        var paths = Directory.GetFiles(Path.GetDirectoryName(file)!, "*.dll")
            .Where(_ => Path.GetFileNameWithoutExtension(_) != typeof(UnrelatedAttribute).Assembly.GetName().Name)
            .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        _context = new MetadataLoadContext(new CanonicalAssemblyResolver(paths));
        _assembly = _context.LoadFromAssemblyPath(file);
    }

    void Destroy() => _context.Dispose();

    [Fact]
    void should_not_resolve_an_unrelated_attribute_on_an_unowned_type()
    {
        _assembly.GetType(typeof(Unowned).FullName!)!.IsQuery().ShouldBeFalse();
        _context.GetAssemblies().Any(_ => _.GetName().Name == typeof(UnrelatedAttribute).Assembly.GetName().Name).ShouldBeFalse();
    }

    [Fact]
    void should_still_reject_an_actual_query_on_an_unowned_type() =>
        Catch.Exception(() => _assembly.GetType(typeof(InvalidOwner).FullName!)!.IsQuery()).GetType().FullName.ShouldEqual("Cratis.Arc.Queries.ModelBound.ModelBoundQueryMethod+InvalidQueryDeclaration");

    [Fact]
    void should_read_an_explicit_owner_and_query_after_unrelated_attributes() =>
        _assembly.GetType(typeof(ExplicitOwner).FullName!)!.GetQueryMethods().Single().Name.ShouldEqual(nameof(ExplicitOwner.All));

    [Fact]
    void should_probe_attribute_type_definitions_as_well_as_references() =>
        _assembly.GetType(typeof(ValidReadModel).FullName!)!.IsQuery().ShouldBeTrue();

    [Fact]
    void should_preserve_selection_for_in_memory_metadata_images()
    {
        var file = typeof(when_probing_metadata_markers).Assembly.Location;
        var paths = Directory.GetFiles(Path.GetDirectoryName(file)!, "*.dll")
            .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        using var context = new MetadataLoadContext(new CanonicalAssemblyResolver(paths));
        var image = context.LoadFromByteArray(File.ReadAllBytes(file));
        image.GetType(typeof(ExplicitOwner).FullName!)!.GetQueryMethods().Single().Name.ShouldEqual(nameof(ExplicitOwner.All));
    }

    [Unrelated]
    public sealed class Unowned
    {
        [Unrelated]
        public static string Helper() => string.Empty;
    }

    [Unrelated]
    public sealed class InvalidOwner
    {
        [Unrelated, Query]
        public static InvalidOwner All() => new();
    }

    [Unrelated, Cratis.Arc.Queries.ModelBound.ReadModel(RequireQueryAttribute = true)]
    public record ExplicitOwner
    {
        [Unrelated, Query]
        public static ExplicitOwner All() => new();
        public static ExplicitOwner Helper() => new();
    }
}
