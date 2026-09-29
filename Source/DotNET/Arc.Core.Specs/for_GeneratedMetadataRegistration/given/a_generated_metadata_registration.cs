// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.given;

public class a_generated_metadata_registration : Specification
{
    protected Func<AssemblyName, Assembly> _loadAssembly;
    protected Func<IEnumerable<string>> _getDependencyContextProjectNames;
    protected Assembly _entryAssembly;
    protected Assembly _otherExecutable;
    protected Module _initializedModule;
    internal GeneratedMetadataRegistration _registration;

    void Establish()
    {
        _loadAssembly = Substitute.For<Func<AssemblyName, Assembly>>();

        // A module whose initializers have long since run, so running them again is the no-op the runtime guarantees
        // and the specification observes only which assemblies were asked for.
        _initializedModule = typeof(a_generated_metadata_registration).Module;
        _loadAssembly(Arg.Any<AssemblyName>()).Returns(typeof(a_generated_metadata_registration).Assembly);
        _getDependencyContextProjectNames = Substitute.For<Func<IEnumerable<string>>>();
        _getDependencyContextProjectNames().Returns([]);
        _entryAssembly = typeof(object).Assembly;
        _otherExecutable = typeof(Specification).Assembly;
        _registration = new(_loadAssembly, _getDependencyContextProjectNames, () => _entryAssembly);
    }

    protected static Dictionary<string, Func<Module>> Modules(params (string Name, Func<Module> GetModule)[] modules) =>
        modules.ToDictionary(_ => _.Name, _ => _.GetModule);
}
