// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.given;

[Authorize]
public record TypeWithAuthorizationAndMethodWithoutAuthorization()
{
    public static void Method()
    {
    }
}
