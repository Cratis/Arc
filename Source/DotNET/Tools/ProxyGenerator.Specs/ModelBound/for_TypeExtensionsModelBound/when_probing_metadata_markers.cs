// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Cratis.Arc.ProxyGenerator.Specs.ActualDependency;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_probing_metadata_markers : Specification
{
    const string InvalidOwnerName = "MetadataFixtures.InvalidOwner";
    MetadataLoadContext _context;
    Assembly _assembly;
    Assembly _invalidOwnerAssembly;
    string _fixtureDirectory;

    void Establish()
    {
        var file = typeof(when_probing_metadata_markers).Assembly.Location;
        var paths = Directory.GetFiles(Path.GetDirectoryName(file)!, "*.dll")
            .Where(_ => Path.GetFileNameWithoutExtension(_) != typeof(UnrelatedAttribute).Assembly.GetName().Name)
            .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        _context = new MetadataLoadContext(new CanonicalAssemblyResolver(paths));
        _assembly = _context.LoadFromAssemblyPath(file);
        _fixtureDirectory = Directory.CreateDirectory(Path.Combine(".ai-work", "3163", "metadata-marker-specs", Guid.NewGuid().ToString("N"))).FullName;
        _invalidOwnerAssembly = _context.LoadFromAssemblyPath(CreateInvalidOwnerImage());
    }

    void Destroy()
    {
        _context.Dispose();
        Directory.Delete(_fixtureDirectory, recursive: true);
    }

    [Fact]
    void should_not_resolve_an_unrelated_attribute_on_an_unowned_type()
    {
        _assembly.GetType(typeof(Unowned).FullName!)!.IsQuery().ShouldBeFalse();
        _context.GetAssemblies().Any(_ => _.GetName().Name == typeof(UnrelatedAttribute).Assembly.GetName().Name).ShouldBeFalse();
    }

    [Fact]
    void should_still_reject_an_actual_query_on_an_unowned_type() =>
        Catch.Exception(() => _invalidOwnerAssembly.GetType(InvalidOwnerName)!.IsQuery()).GetType().FullName.ShouldEqual("Cratis.Arc.Queries.ModelBound.ModelBoundQueryMethod+InvalidQueryDeclaration");

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

    string CreateInvalidOwnerImage()
    {
        // Write the negative fixture without loading it into runtime discovery, including on .NET 8.
        // Keep the unrelated attribute reference even though its dependency is absent from the resolver.
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("InvalidMetadataQueryFixture.dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("InvalidMetadataQueryFixture"), new Version(1, 0, 0, 0), default, default, default, AssemblyHashAlgorithm.None);
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        var type = metadata.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Sealed,
            metadata.GetOrAddString("MetadataFixtures"),
            metadata.GetOrAddString("InvalidOwner"),
            AddTypeReference(metadata, typeof(object)),
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));
        var signature = new BlobBuilder();
        new BlobEncoder(signature).MethodSignature().Parameters(0, _ => _.Type().Type(type, isValueType: false), _ => { });
        var instructions = new BlobBuilder();
        var body = new InstructionEncoder(instructions);
        body.OpCode(ILOpCode.Ldnull);
        body.OpCode(ILOpCode.Ret);
        var bodies = new BlobBuilder();
        var bodyOffset = new MethodBodyStreamEncoder(bodies).AddMethodBody(body);
        var method = metadata.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.Static,
            MethodImplAttributes.IL,
            metadata.GetOrAddString("All"),
            metadata.GetOrAddBlob(signature),
            bodyOffset,
            MetadataTokens.ParameterHandle(1));
        var unrelated = AddAttributeConstructor(metadata, typeof(UnrelatedAttribute));
        var attributeValue = metadata.GetOrAddBlob(new byte[] { 1, 0, 0, 0 });
        metadata.AddCustomAttribute(type, unrelated, attributeValue);
        metadata.AddCustomAttribute(method, unrelated, attributeValue);
        metadata.AddCustomAttribute(method, AddAttributeConstructor(metadata, typeof(QueryAttribute)), attributeValue);
        var image = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.Dll), new MetadataRootBuilder(metadata), bodies);
        var blob = new BlobBuilder();
        image.Serialize(blob);
        var file = Path.Combine(_fixtureDirectory, "InvalidMetadataQueryFixture.dll");
        File.WriteAllBytes(file, blob.ToArray());
        return file;
    }

    static TypeReferenceHandle AddTypeReference(MetadataBuilder metadata, Type type)
    {
        var name = type.Assembly.GetName();
        var assembly = metadata.AddAssemblyReference(
            metadata.GetOrAddString(name.Name!),
            name.Version!,
            default,
            metadata.GetOrAddBlob(name.GetPublicKeyToken()!),
            default,
            default);
        return metadata.AddTypeReference(assembly, metadata.GetOrAddString(type.Namespace!), metadata.GetOrAddString(type.Name));
    }

    static MemberReferenceHandle AddAttributeConstructor(MetadataBuilder metadata, Type type)
    {
        var signature = new BlobBuilder();
        new BlobEncoder(signature).MethodSignature(isInstanceMethod: true).Parameters(0, _ => _.Void(), _ => { });
        return metadata.AddMemberReference(AddTypeReference(metadata, type), metadata.GetOrAddString(".ctor"), metadata.GetOrAddBlob(signature));
    }

    [Unrelated, Cratis.Arc.Queries.ModelBound.ReadModel(RequireQueryAttribute = true)]
    public record ExplicitOwner
    {
        [Unrelated, Query]
        public static ExplicitOwner All() => new();
        public static ExplicitOwner Helper() => new();
    }
}
