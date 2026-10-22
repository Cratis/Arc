// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_MetadataQueryAttributes.given;

public class a_replaceable_module : Specification
{
    protected string _path;
    string _directory;

    void Establish()
    {
        _directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-arc-discovery-specs", Guid.NewGuid().ToString("N"))).FullName;
        _path = Path.Combine(_directory, "ReplaceableModule.dll");
    }

    void Destroy() => Directory.Delete(_directory, recursive: true);

    protected MetadataLoadContext CreateContext()
    {
        var paths = Directory.GetFiles(Path.GetDirectoryName(typeof(ReadModelAttribute).Assembly.Location)!, "*.dll")
            .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        return new MetadataLoadContext(new CanonicalAssemblyResolver(paths));
    }

    protected void WriteModule(bool marked)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("ReplaceableModule.dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("ReplaceableModule"), new Version(1, 0, 0, 0), default, default, default, AssemblyHashAlgorithm.None);
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        var type = metadata.AddTypeDefinition(TypeAttributes.Public, default, metadata.GetOrAddString("Owner"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        if (marked)
        {
            AddMarker(metadata, type);
        }

        var image = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.Dll), new MetadataRootBuilder(metadata), new BlobBuilder());
        var blob = new BlobBuilder();
        image.Serialize(blob);
        File.WriteAllBytes(_path, blob.ToArray());
    }

    static void AddMarker(MetadataBuilder metadata, TypeDefinitionHandle type)
    {
        var name = typeof(ReadModelAttribute).Assembly.GetName();
        var assembly = metadata.AddAssemblyReference(metadata.GetOrAddString(name.Name!), name.Version!, default, metadata.GetOrAddBlob(name.GetPublicKeyToken()!), default, default);
        var attribute = metadata.AddTypeReference(assembly, metadata.GetOrAddString(typeof(ReadModelAttribute).Namespace!), metadata.GetOrAddString(nameof(ReadModelAttribute)));
        var signature = new BlobBuilder();
        new BlobEncoder(signature).MethodSignature(isInstanceMethod: true).Parameters(0, _ => _.Void(), _ => { });
        var constructor = metadata.AddMemberReference(attribute, metadata.GetOrAddString(".ctor"), metadata.GetOrAddBlob(signature));
        metadata.AddCustomAttribute(type, constructor, metadata.GetOrAddBlob(new byte[] { 1, 0, 0, 0 }));
    }
}
