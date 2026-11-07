// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.for_PatAdapterSurface;

public class when_printing_a_record : for_PersonalAccessTokens.given.a_pat_module
{
    string _recordText;
    string _intentText;

    async Task Because()
    {
        await Issue();
        _recordText = _record!.ToString();
        _intentText = _intent!.ToString();
    }

    [Fact] void should_omit_the_verifier_from_record_text() => _recordText.Contains(_record!.Verifier, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_omit_the_verifier_from_intent_text() => _intentText.Contains(_record!.Verifier, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_preserve_the_module_created_intent() => _intent.ShouldEqual(new PatIssuedIntent(_record!.CredentialId, Now));
}
