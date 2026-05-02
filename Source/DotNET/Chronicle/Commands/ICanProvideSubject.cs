// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Provides the subject for an event append.
/// </summary>
public interface ICanProvideSubject
{
    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The <see cref="Subject"/>.</returns>
    Subject GetSubject();
}
