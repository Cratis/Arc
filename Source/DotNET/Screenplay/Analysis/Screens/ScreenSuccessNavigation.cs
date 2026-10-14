// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Keeps dialog success navigation on command actions, separate from their toolbar input openers.
/// </summary>
/// <param name="callbacks">The proven router callback reader.</param>
/// <param name="navigation">The literal route resolver.</param>
/// <param name="unread">The screen omission reporter.</param>
internal sealed class ScreenSuccessNavigation(MenuCallbacks callbacks, Func<string, ScreenNavigationModel?>? navigation, UnreadScreenStructure unread)
{
    readonly Dictionary<string, List<ScreenNavigationModel?>> _destinations = new(StringComparer.Ordinal);

    /// <summary>
    /// Records the success behavior of every dialog for a command, reporting unread callbacks immediately.
    /// </summary>
    /// <param name="dialog">The dialog element.</param>
    /// <param name="command">The command reference.</param>
    internal void Read(JsxElement dialog, string command)
    {
        var callback = dialog["onSuccess"];
        var route = callback is { Literal: null } ? callbacks.Route(callback.Written, dialog.Position) : null;
        var destination = route is not null ? navigation?.Invoke(route) : null;
        if (callback is not null && destination is null)
        {
            unread.AfterSuccess(command, callback.Written);
        }

        if (!_destinations.TryGetValue(command, out var destinations))
        {
            _destinations[command] = destinations = [];
        }

        destinations.Add(destination);
    }

    /// <summary>
    /// Gets only success destinations shared by every dialog exposing the same action.
    /// </summary>
    /// <returns>The unambiguous action navigation.</returns>
    internal IReadOnlyDictionary<string, ScreenNavigationModel> Resolved() => _destinations
        .Select(entry => (entry.Key, Navigation: Resolve(entry.Key, entry.Value)))
        .Where(_ => _.Navigation is not null).ToDictionary(_ => _.Key, _ => _.Navigation!, StringComparer.Ordinal);

    static bool Agrees(ScreenNavigationModel left, ScreenNavigationModel right) =>
        left.Screen == right.Screen && left.Route == right.Route && left.Parameters.Count == right.Parameters.Count &&
        left.Parameters.All(parameter => right.Parameters.TryGetValue(parameter.Key, out var value) && parameter.Value == value);

    ScreenNavigationModel? Resolve(string command, List<ScreenNavigationModel?> destinations)
    {
        var known = destinations.OfType<ScreenNavigationModel>().ToList();
        if (known.Count == 0)
        {
            return null;
        }

        if (known.Count == destinations.Count && known.TrueForAll(_ => Agrees(_, known[0])))
        {
            return known[0];
        }

        unread.AfterSuccess(command, "multiple dialogs with different post-success behavior");
        return null;
    }
}
