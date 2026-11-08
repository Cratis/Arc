// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Marks PAT-capable capability endpoints without exposing a caller-construction seam.</summary>
[IgnoreConvention]
internal sealed class PatCapableCallerInstallation;
