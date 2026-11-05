// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal sealed record TestKey(string Name, string[] KeyIds, string PrivateKeyPem, string PublicKeyPem);
