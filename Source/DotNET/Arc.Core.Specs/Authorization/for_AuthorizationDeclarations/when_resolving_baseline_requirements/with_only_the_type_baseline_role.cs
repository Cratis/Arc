// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class with_only_the_type_baseline_role : given.a_baseline
{
    bool _authorized;

    void Because() => _authorized = _evaluator.IsAuthorized(typeof(Plain).GetMethod(nameof(Plain.Run))!);

    [Fact] void should_enforce_both_fallbacks_when_evaluating_the_principal() => _authorized.ShouldBeFalse();
}
