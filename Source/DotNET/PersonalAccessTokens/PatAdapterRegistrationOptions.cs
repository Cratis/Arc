// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Holds the original service collection for validation at host start.</summary>
internal sealed class PatAdapterRegistrationOptions
{
    /// <summary>Gets or sets the installation service descriptors.</summary>
    internal IServiceCollection Services { get; set; } = null!;
}
