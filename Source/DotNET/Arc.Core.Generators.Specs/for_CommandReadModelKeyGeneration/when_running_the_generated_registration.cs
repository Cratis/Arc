// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_running_the_generated_registration : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        namespace Run;
        public class Base { public string First { get; set; } = "first"; }
        [Command]
        public class Change : Base
        {
            public System.Guid AccountId { get; set; } = System.Guid.NewGuid();
            public string Own { get; set; } = "own";
            public string Last { get; set; } = "last";
            public void Handle(Account account) { }
        }
        public class Account { public System.Guid Id { get; set; } }
        """;

    Assembly _assembly;
    Type _command;
    object[] _registered;

    void Establish()
    {
        var (_, diagnostics, output) = CommandKeyCompilation.Compile("RunRegistration", Models);
        diagnostics.ShouldBeEmpty();
        _assembly = ModelGraphWalkerCompilation.Load(output);
        _command = _assembly.GetType("Run.Change")!;
    }

    void Because() => _registered = [.. ((Array)CommandKeyCompilation.Registered(_assembly, "Commands")[_command]!).Cast<object>()];

    [Fact] void should_register_the_properties_in_reflection_order() =>
        _registered.Select(property => (string)property.GetType().GetProperty("Name")!.GetValue(property)!)
            .ShouldEqual(_command.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name));
    [Fact] void should_register_typed_getters_that_read_the_command()
    {
        var instance = Activator.CreateInstance(_command)!;
        var own = _registered.Single(property => (string)property.GetType().GetProperty("Name")!.GetValue(property)! == "Own");
        var read = (Func<object, object?>)own.GetType().GetProperty("Read")!.GetValue(own)!;
        read(instance).ShouldEqual("own");
    }
    [Fact] void should_register_the_property_types()
    {
        var account = _registered.Single(property => (string)property.GetType().GetProperty("Name")!.GetValue(property)! == "AccountId");
        account.GetType().GetProperty("PropertyType")!.GetValue(account).ShouldEqual(typeof(Guid));
    }
    [Fact] void should_declare_the_model_key() => CommandKeyCompilation.Registered(_assembly, "Models")[_assembly.GetType("Run.Account")!].ShouldEqual(typeof(Guid));
}
