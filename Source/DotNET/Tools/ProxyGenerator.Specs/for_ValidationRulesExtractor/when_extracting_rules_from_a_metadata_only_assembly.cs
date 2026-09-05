// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// The generator never sees runtime types: <c>TypeExtensions.InitializeProjectAssemblies</c> loads every project
/// assembly through a <see cref="MetadataLoadContext"/>, so this is the only world the extractor actually runs in.
/// Rules that can only be read by instantiating a validator are therefore invisible in production even though every
/// runtime-typed spec passes.
/// </summary>
public class when_extracting_rules_from_a_metadata_only_assembly : Specification
{
    MetadataLoadContext _context;
    IEnumerable<PropertyValidationDescriptor> _fromCommandValidator;
    IEnumerable<PropertyValidationDescriptor> _fromConceptValidator;

    void Establish()
    {
        // Mirrors TypeExtensions.InitializeProjectAssemblies so the spec resolves exactly what the generator does.
        var assemblyFile = typeof(TestCommand).Assembly.Location;
        var runtimeDirectory = Path.GetDirectoryName(RuntimeEnvironment.GetRuntimeDirectory())!;
        var version = Path.GetFileName(runtimeDirectory);
        var shared = Directory.GetParent(Directory.GetParent(runtimeDirectory)!.FullName)!;
        var aspNetCoreDirectory = Path.Combine(shared.FullName, "Microsoft.AspNetCore.App", version);

        string[] paths =
        [
            .. Directory.GetFiles(runtimeDirectory, "*.dll"),
            .. Directory.GetFiles(aspNetCoreDirectory, "*.dll"),
            .. Directory.GetFiles(Path.GetDirectoryName(assemblyFile)!, "*.dll")
        ];

        _context = new MetadataLoadContext(new PathAssemblyResolver(paths.Distinct(new FileNameComparer())));
    }

    void Because()
    {
        var assembly = _context.LoadFromAssemblyPath(typeof(TestCommand).Assembly.Location);
        _fromCommandValidator = ValidationRulesExtractor.ExtractValidationRules(
            assembly,
            assembly.GetType(typeof(TestCommand).FullName!)!);
        _fromConceptValidator = ValidationRulesExtractor.ExtractValidationRules(
            assembly,
            assembly.GetType(typeof(TestCommandWithConcept).FullName!)!);
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_extract_the_rules_declared_on_the_command_validator() => _fromCommandValidator.Select(_ => _.PropertyName).ShouldContain("name");
    [Fact] void should_extract_the_rules_contributed_by_a_concept_validator() => _fromConceptValidator.Select(_ => _.PropertyName).ShouldContain("email");
}
