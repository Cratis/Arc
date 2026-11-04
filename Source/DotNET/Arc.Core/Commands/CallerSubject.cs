// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>Resolves the selected caller's exact <c>sub</c> claim, without claim-type mapping.</summary>
public sealed class CallerSubject() : CallerClaim("sub");
