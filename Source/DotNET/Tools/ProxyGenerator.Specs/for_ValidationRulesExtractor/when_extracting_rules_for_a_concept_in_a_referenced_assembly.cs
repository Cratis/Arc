// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class CommandWithReferencedConcept
{
    public ReferencedEmail Email { get; set; } = new(string.Empty);
}

/// <summary>
/// A command in the generated assembly must carry the rules of a concept validator from its referenced assembly.
/// </summary>
public class when_extracting_rules_for_a_concept_in_a_referenced_assembly : Specification
{
    MetadataLoadContext _context;
    IEnumerable<PropertyValidationDescriptor> _result;

    void Establish()
    {
        var assemblyFile = typeof(CommandWithReferencedConcept).Assembly.Location;
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
        var assembly = _context.LoadFromAssemblyPath(typeof(CommandWithReferencedConcept).Assembly.Location);
        _result = ValidationRulesExtractor.ExtractValidationRules(
            assembly,
            assembly.GetType(typeof(CommandWithReferencedConcept).FullName!)!);
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_include_the_referenced_concept_validator_rule() => _result.Single(_ => _.PropertyName == "email").Rules.Single().RuleName.ShouldEqual("emailAddress");
    [Fact] void should_include_the_referenced_concept_validator_message() => _result.Single(_ => _.PropertyName == "email").Rules.Single().ErrorMessage.ShouldEqual("Referenced email is invalid");
}
