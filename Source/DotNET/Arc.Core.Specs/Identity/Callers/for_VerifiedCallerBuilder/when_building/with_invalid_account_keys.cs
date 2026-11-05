// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_invalid_account_keys : given.a_builder
{
    CallerConstruction[] _results;

    void Because()
    {
        var account = _facts.AccountKey!;
        CallerAccountKey?[] invalid =
        [
            null,
            account with { Issuer = "http://login.example.test" },
            account with { Issuer = "/login" },
            account with { Subject = "8B167AD2-0F89-4C78-9D42-6D4580495D32" },
            account with { Subject = "8b167ad20f894c789d426d4580495d32" },
            account with { Subject = "8b167ad2-0f89-1c78-9d42-6d4580495d32" },
            account with { Subject = "8b167ad2-0f89-4c78-0d42-6d4580495d32" },
            account with { Subject = string.Empty }
        ];
        _results = invalid.Select(value => new VerifiedCallerBuilder(_catalog).Build(_facts with { AccountKey = value }, _installation, _tenant)).ToArray();
    }

    [Fact] void should_require_https_issuer_and_canonical_uuid_v4() => _results.All(result => result.Refusal == CallerRefusal.InvalidAccountKey).ShouldBeTrue();
    [Fact] void should_never_normalize_an_invalid_account_into_a_caller() => _results.All(result => result.Caller is null).ShouldBeTrue();
}
