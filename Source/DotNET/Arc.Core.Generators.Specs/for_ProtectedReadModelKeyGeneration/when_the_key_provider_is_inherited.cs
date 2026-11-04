// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

namespace Cratis.Arc.Generators.Specs.for_ProtectedReadModelKeyGeneration;

public class when_the_key_provider_is_inherited : Specification
{
    const string Models = """
        namespace Keys
        {
            [Cratis.Arc.Queries.ModelBound.ReadModel]
            public record Model(System.Guid Id);
            public class BaseCommand
            {
                public virtual void Handle([Cratis.Arc.Commands.LookUpBy<Cratis.Arc.Commands.CallerSubject>] Model model) { }
            }
            [Cratis.Arc.Commands.ModelBound.Command]
            public class Change : BaseCommand
            {
                public System.Guid Candidate { get; set; }
                [Cratis.Chronicle.Keys.Key] public int Declared { get; set; }
                public override void Handle(Model model) { }
            }
        }
        """;

    string _source;

    void Because() => (_source, _) = CompositeKeyCompilation.Compile("InheritedProviderKeys", Models);

    [Fact] void should_keep_explicit_command_keys() => _source.ShouldContain("typeof(int)");
    [Fact] void should_not_infer_the_provider_key_from_command_properties() => _source.ShouldNotContain("typeof(global::System.Guid)");
}
