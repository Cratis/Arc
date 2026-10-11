// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Cratis.Arc.ProxyGenerator.Specs.ActualDependency;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_loading_canonical_metadata_images : Specification
{
    string _directory;

    void Establish() => _directory = Directory.CreateDirectory(Path.Combine(".ai-work", "3163", "metadata-resolver-specs", Guid.NewGuid().ToString("N"))).FullName;

    void Destroy() => Directory.Delete(_directory, recursive: true);

    [Fact]
    void should_use_the_preferred_application_image_for_eager_and_attribute_resolution()
    {
        var original = typeof(UnrelatedAttribute).Assembly;
        var application = Path.Combine(Directory.CreateDirectory(Path.Combine(_directory, "app")).FullName, Path.GetFileName(original.Location));
        var framework = Path.Combine(Directory.CreateDirectory(Path.Combine(_directory, "framework")).FullName, Path.GetFileName(original.Location));
        File.Copy(original.Location, application);
        var bytes = File.ReadAllBytes(original.Location);
        var oldMvid = original.ManifestModule.ModuleVersionId.ToByteArray();
        var offset = bytes.AsSpan().IndexOf(oldMvid);
        offset.ShouldBeGreaterThan(0);
        Guid.NewGuid().ToByteArray().CopyTo(bytes, offset);
        File.WriteAllBytes(framework, bytes);
        AssemblyName.GetAssemblyName(application).FullName.ShouldEqual(AssemblyName.GetAssemblyName(framework).FullName);

        var resolver = new CanonicalAssemblyResolver(
            [framework, .. Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")],
            [application, typeof(Attributed).Assembly.Location]);
        using var context = new MetadataLoadContext(resolver);
        var preloaded = resolver.Load(context, application);
        var owner = context.LoadFromAssemblyPath(typeof(Attributed).Assembly.Location).GetType(typeof(Attributed).FullName!)!;
        var resolved = owner.GetCustomAttributesData().Single().AttributeType.Assembly;
        ReferenceEquals(preloaded, resolved).ShouldBeTrue();
        resolved.ManifestModule.ModuleVersionId.ShouldEqual(original.ManifestModule.ModuleVersionId);
        resolver.Load(context, framework).ShouldEqual(preloaded);
    }

    [Fact]
    void should_preserve_distinct_versions_even_with_the_same_file_name()
    {
        var first = Image("one", new Version(1, 0, 0, 0));
        var second = Image("two", new Version(2, 0, 0, 0));
        var resolver = new CanonicalAssemblyResolver([first, second, .. Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")]);
        using var context = new MetadataLoadContext(resolver);
        resolver.Load(context, first).GetName().Version.ShouldEqual(new Version(1, 0, 0, 0));
        resolver.Load(context, second).GetName().Version.ShouldEqual(new Version(2, 0, 0, 0));
    }

    [Fact]
    void should_use_framework_fallback_without_an_application_image()
    {
        var framework = Image("framework", new Version(1, 0, 0, 0));
        var resolver = new CanonicalAssemblyResolver([framework, .. Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")]);
        using var context = new MetadataLoadContext(resolver);
        context.LoadFromAssemblyName(AssemblyName.GetAssemblyName(framework)).Location.ShouldEqual(framework);
    }

    [Fact]
    void should_skip_a_valid_native_framework_image_before_reading_assembly_identity()
    {
        var native = NativeImage();
        using var stream = File.OpenRead(native);
        using var image = new PEReader(stream);
        image.PEHeaders.PEHeader.ShouldNotBeNull();
        image.PEHeaders.CorHeader.ShouldBeNull();
        image.HasMetadata.ShouldBeFalse();
        image.PEHeaders.SectionHeaders.Single().Name.ShouldEqual(".text");
        var framework = Image("framework", new Version(1, 0, 0, 0));
        var resolver = new CanonicalAssemblyResolver([native, framework, .. Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")]);
        using var context = new MetadataLoadContext(resolver);
        context.LoadFromAssemblyName(AssemblyName.GetAssemblyName(framework)).Location.ShouldEqual(framework);
    }

    [Fact]
    void should_reject_a_native_image_declared_as_a_required_managed_dependency() =>
        Catch.Exception(() => _ = new CanonicalAssemblyResolver([], [NativeImage()])).ShouldBeOfExactType<BadImageFormatException>();

    [Fact]
    void should_fail_when_a_required_managed_dependency_is_missing() =>
        Catch.Exception(() => _ = new CanonicalAssemblyResolver([], [Path.Combine(_directory, "Missing.dll")])).ShouldBeOfExactType<FileNotFoundException>();

    [Fact]
    void should_fail_when_a_required_managed_dependency_is_corrupt()
    {
        var file = Path.Combine(_directory, "Corrupt.dll");
        File.WriteAllText(file, "not a portable executable");
        Catch.Exception(() => _ = new CanonicalAssemblyResolver([], [file])).ShouldBeOfExactType<BadImageFormatException>();
    }

    [Fact]
    void should_not_silently_skip_a_corrupt_directory_candidate()
    {
        var file = Path.Combine(_directory, "Corrupt.dll");
        File.WriteAllText(file, "not a portable executable");
        Catch.Exception(() => _ = new CanonicalAssemblyResolver([file])).ShouldBeOfExactType<BadImageFormatException>();
    }

    string NativeImage()
    {
        var file = Path.Combine(_directory, "NativeDependency.dll");
        var blob = new BlobBuilder();
        new NativeImageBuilder().Serialize(blob);
        File.WriteAllBytes(file, blob.ToArray());
        return file;
    }

    string Image(string folder, Version version)
    {
        var file = Path.Combine(Directory.CreateDirectory(Path.Combine(_directory, folder)).FullName, "IdentityFixture.dll");
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("IdentityFixture.dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("IdentityFixture"), version, default, default, default, AssemblyHashAlgorithm.None);
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        var image = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.Dll), new MetadataRootBuilder(metadata), new BlobBuilder());
        var blob = new BlobBuilder();
        image.Serialize(blob);
        File.WriteAllBytes(file, blob.ToArray());
        return file;
    }

    [Unrelated]
    public class Attributed;

    sealed class NativeImageBuilder() : PEBuilder(new PEHeaderBuilder(machine: Machine.Amd64, imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll), deterministicIdProvider: null)
    {
        protected override ImmutableArray<Section> CreateSections() => [new(".text", SectionCharacteristics.ContainsCode | SectionCharacteristics.MemExecute | SectionCharacteristics.MemRead)];

        protected override PEDirectoriesBuilder GetDirectories() => new();

        protected override BlobBuilder SerializeSection(string name, SectionLocation location)
        {
            var blob = new BlobBuilder();
            blob.WriteByte(0xc3); // Native x64 ret instruction; no CLR directory or metadata is emitted.
            return blob;
        }
    }
}
