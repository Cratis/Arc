// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

namespace Cratis.Arc.Generators.Specs.for_ProtectedReadModelKeyGeneration;

public class when_parameter_has_a_key_provider : Specification
{
    const string Models = """
        namespace Keys
        {
            [Cratis.Arc.Queries.ModelBound.ReadModel]
            public record Model(System.Guid Id);
            [Cratis.Arc.Commands.ModelBound.Command]
            public record Change(System.Guid Candidate, [Cratis.Chronicle.Keys.Key] int Declared)
            {
                public void Handle([Cratis.Arc.Commands.LookUpBy<Cratis.Arc.Commands.CallerSubject>] Model model) { }
            }
        }
        """;

    string _source;

    void Because() => (_source, _) = CompositeKeyCompilation.Compile("ProviderKeys", Models);

    [Fact] void should_keep_explicit_command_keys() => _source.ShouldContain("typeof(int)");
    [Fact] void should_not_infer_the_provider_key_from_command_properties() => _source.ShouldNotContain("typeof(global::System.Guid)");
}
