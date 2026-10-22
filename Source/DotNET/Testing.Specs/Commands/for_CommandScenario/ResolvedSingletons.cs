// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Testing.for_CommandScenario;

public record ResolvedSingletons(CurrentPrincipalAccessor PrincipalAccessor, IAuthorizationPolicyRuntime PolicyRuntime);
