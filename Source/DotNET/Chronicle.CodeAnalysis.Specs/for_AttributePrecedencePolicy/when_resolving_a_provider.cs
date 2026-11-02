// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy;

public class when_resolving_a_provider : given.a_declaration
{
    [Theory]
    [InlineData("LookUpBy(\"Key\"), LookUpBy<CallerSubject>")]
    public void should_refuse_conflicting_forms_in_reflection_and_roslyn(string attributes)
    {
        _action = "lookup";
        _source = $"public class Command {{ public string Key {{ get; }} public void Handle([{attributes}] Model model) {{ }} }} public class Model; public sealed class LoginClaim() : CallerClaim(\"login\");";

        Resolve();

        _runtime.ShouldEqual("InvalidLookUpBy");
        _roslyn.ShouldEqual("InvalidLookUpBy");
    }
}
