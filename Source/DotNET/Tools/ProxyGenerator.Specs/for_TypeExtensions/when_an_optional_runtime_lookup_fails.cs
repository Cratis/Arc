// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_an_optional_runtime_lookup_fails : for_Generator.given.a_validator_runtime_application
{
    Assembly? _optionalAssembly;
    bool _returnedOriginalType;
    Exception? _requiredError;
    bool _metadataOnly;

    void Because()
    {
        var runtimeAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var applicationAssemblies = Directory.GetFiles(Path.GetDirectoryName(ApplicationPath)!, "*.dll");
        using var context = new MetadataLoadContext(new PathAssemblyResolver(runtimeAssemblies.Concat(applicationAssemblies)));
        var assembly = context.LoadFromAssemblyPath(ApplicationPath);
        var type = assembly.GetType("ValidatorRuntimeApplication.CreateApplication")!;
        _metadataOnly = assembly.ReflectionOnly;
        File.Delete(ApplicationPath);
        _optionalAssembly = RuntimeValidatorAssemblies.For(assembly);
        _returnedOriginalType = ReferenceEquals(RuntimeValidatorAssemblies.For(type), type);
        _requiredError = Catch.Exception(() => RuntimeValidatorAssemblies.RequiredForGeneration(type));
    }

    [Fact] void should_initialize_only_a_metadata_graph() => _metadataOnly.ShouldBeTrue();
    [Fact] void should_preserve_the_optional_assembly_contract() => _optionalAssembly.ShouldBeNull();
    [Fact] void should_preserve_the_optional_type_contract() => _returnedOriginalType.ShouldBeTrue();
    [Fact] void should_not_hide_a_subsequent_required_failure() => _requiredError.ShouldBeOfExactType<ValidatorRuntimeCouldNotBeLoaded>();
    [Fact] void should_retain_the_underlying_cause() => _requiredError!.GetBaseException().ShouldBeOfExactType<FileNotFoundException>();
}
