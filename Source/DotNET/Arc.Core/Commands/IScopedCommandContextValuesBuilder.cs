// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Builds command values from providers resolved in the command's execution scope.
/// </summary>
internal interface IScopedCommandContextValuesBuilder : ICommandContextValuesBuilder
{
    /// <summary>
    /// Builds values without capturing identity-bound providers from the pipeline's root scope.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="services">The owned execution provider.</param>
    /// <returns>The context values.</returns>
    CommandContextValues Build(object command, IServiceProvider services);
}
