// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Envelope;

public class when_parsing : Specification
{
    readonly List<string> _mismatches = [];
    ForwardedIdentityV2Envelope _valid;

    void Because()
    {
        var valid = given.ForwardedIdentityV2Vectors.Named("valid-v2").Envelope;
        var parts = valid.Split('.');
        foreach (var text in new[] { "", new string('a', 257), valid + " ", valid + "\r", string.Join('.', parts.Take(4)), valid + ".extra", valid.Replace(parts[1], "!"), string.Join('.', parts.Select((part, index) => index == 1 ? "" : part)), valid.Replace(parts[1], new string('a', 65)), valid.Replace(parts[2], "+1"), valid.Replace(parts[2], "01"), valid.Replace(parts[2], "9223372036854775808"), valid.Replace(parts[3], parts[3] + "="), valid.Replace(parts[3], "A"), valid.Replace(parts[3], parts[3][..^1] + "B"), valid.Replace(parts[4], parts[4] + "="), valid.Replace(parts[4], "A"), valid.Replace(parts[4], parts[4][..^1] + "B") })
        {
            if (ForwardedIdentityV2Envelope.Parse(text).Rejection != ForwardedIdentityV2Rejection.Format) _mismatches.Add(text);
        }
        foreach (var version in new[] { "v1", "v3" })
        {
            if (ForwardedIdentityV2Envelope.Parse(valid.Replace("v2.", version + ".")).Rejection != ForwardedIdentityV2Rejection.Version) _mismatches.Add(version);
        }
        _valid = ForwardedIdentityV2Envelope.Parse(valid).Envelope!;
    }

    [Fact] void should_refuse_invalid_representations() => _mismatches.ShouldBeEmpty();
    [Fact] void should_preserve_valid_parts() => _valid.Parts[0].ShouldEqual("v2");
    [Fact] void should_decode_a_fixed_width_signature() => _valid.Signature.Length.ShouldEqual(64);
}
