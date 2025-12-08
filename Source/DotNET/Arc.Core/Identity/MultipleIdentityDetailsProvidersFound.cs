// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Exception that gets thrown when there are <see cref="IProvideIdentityDetails">multiple identity details providers</see>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MultipleIdentityDetailsProvidersFound"/> class.
/// </remarks>
/// <param name="types">Types that were found.</param>
public class MultipleIdentityDetailsProvidersFound(IEnumerable<Type> types) : Exception($"There should only be one implementation of `{nameof(IProvideIdentityDetails)}` found {string.Join(',', types.Select(_ => _.FullName))}")
{
}
