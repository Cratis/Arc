// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Carries the common literal route properties recovered from JSX or router object literals.
/// </summary>
/// <param name="Properties">The literal field or attribute values.</param>
/// <param name="Children">The nested route nodes.</param>
/// <param name="IsReadable">Whether the container and its children are fully literal.</param>
internal sealed record RouteNode(IReadOnlyDictionary<string, JsxValue> Properties, IReadOnlyList<RouteNode> Children, bool IsReadable)
{
    /// <summary>
    /// Gets an unread route placeholder rather than silently dropping a computed declaration.
    /// </summary>
    internal static readonly RouteNode Unread = new(new Dictionary<string, JsxValue>(), [], false);
}
