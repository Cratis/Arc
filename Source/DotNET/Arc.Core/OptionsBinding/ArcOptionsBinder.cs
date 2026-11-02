// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Binds <see cref="ArcOptions"/> from configuration without reflection, with the outcome of
/// <c>ConfigurationBinder.Bind</c>: the same values, the same order of assignments and the same failures.
/// </summary>
internal static class ArcOptionsBinder
{
    /// <summary>
    /// Binds configuration onto an existing instance.
    /// </summary>
    /// <param name="options">The options to bind.</param>
    /// <param name="configuration">The Arc configuration section.</param>
    /// <param name="logger">Optional logger for the runtime subtype warning.</param>
    internal static void Bind(ArcOptions options, IConfiguration configuration, ILogger? logger = null)
    {
        // ConfigurationBinder.Bind(configuration, instance) starts at BindInstance with a non-null, read-only point.
        var point = new BindingPoint(options, true);
        RuntimeTypeFallback.UseLogger(logger);
        try
        {
            BinderSemantics.BindInstance(ArcOptionsBinding.Bound, point, configuration, false);
        }
        finally
        {
            RuntimeTypeFallback.UseLogger(null);
        }
    }
}
