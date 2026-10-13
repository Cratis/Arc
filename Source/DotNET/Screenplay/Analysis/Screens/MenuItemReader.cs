// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads data page menus without interpreting arbitrary callbacks or conditional children.
/// </summary>
/// <param name="source">The scanned module.</param>
/// <param name="components">The known component imports.</param>
/// <param name="commands">The commands declared by the slice.</param>
/// <param name="unread">The per-directive reporter.</param>
internal sealed class MenuItemReader(TsxSource source, KnownComponents components, IReadOnlyCollection<CommandModel> commands, UnreadScreenStructure unread)
{
    /// <summary>
    /// The dialog positions already exposed by proven toolbar items.
    /// </summary>
    readonly HashSet<int> _claimedDialogs = [];

    /// <summary>
    /// The supported callback and lexical binding reader.
    /// </summary>
    readonly MenuCallbacks _callbacks = new(source, components);

    /// <summary>
    /// Determines whether a menu contains an item.
    /// </summary>
    /// <param name="menu">The menu.</param>
    /// <param name="item">The item.</param>
    /// <returns>Whether it lies inside the menu's children.</returns>
    internal static bool Contains(JsxElement menu, JsxElement item) => item.Position >= menu.ChildrenStart && item.End <= menu.ChildrenEnd;

    /// <summary>
    /// Chooses a stable item name, adding a numeric suffix only on collision.
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <param name="names">The used names.</param>
    /// <returns>The unique item name.</returns>
    internal static string ItemName(string command, HashSet<string> names)
    {
        var name = command;
        var suffix = 2;
        while (!names.Add(name))
        {
            name = $"{command}{suffix++}";
        }

        return name;
    }

    /// <summary>
    /// Determines whether an attribute is a literal boolean expression rather than quoted text.
    /// </summary>
    /// <param name="value">The attribute value.</param>
    /// <param name="boolean">The expected boolean token.</param>
    /// <returns>Whether it is the boolean token.</returns>
    internal static bool IsBoolean(JsxValue value, string boolean) => value.Literal is null && value.Written == boolean;

    /// <summary>
    /// Determines whether an emitted toolbar item already exposes a dialog.
    /// </summary>
    /// <param name="dialog">The dialog element.</param>
    /// <returns>Whether it has been claimed.</returns>
    internal bool Claims(JsxElement dialog) => _claimedDialogs.Contains(dialog.Position);

    /// <summary>
    /// Reads page menus before dialog actions, with stable names even when a table stays unread.
    /// </summary>
    /// <param name="pages">The pages in source order.</param>
    /// <param name="queries">The slice queries.</param>
    /// <returns>The recovered toolbars.</returns>
    internal IReadOnlyList<ScreenToolbarModel> ReadPages(IReadOnlyList<JsxElement> pages, IReadOnlyCollection<QueryModel> queries)
    {
        var toolbars = new List<ScreenToolbarModel>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in pages.Where(_ => _.IsReadable))
        {
            var name = ItemName(ToolbarName(page, pages, queries), names);
            if (Read(page, name) is { } toolbar)
            {
                toolbars.Add(toolbar);
            }
        }

        return toolbars;
    }

    /// <summary>
    /// Names a toolbar using its read model for a multiple-page screen.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="pages">The screen's pages.</param>
    /// <param name="queries">The slice queries.</param>
    /// <returns>The toolbar name.</returns>
    string ToolbarName(JsxElement page, IReadOnlyList<JsxElement> pages, IReadOnlyCollection<QueryModel> queries)
    {
        if (pages.Count == 1)
        {
            return "actions";
        }

        var proxy = page["query"]?.Identifier is { } local ? components.Proxy(local) : null;
        var query = queries.FirstOrDefault(_ => _.Name == proxy);
        return query is null ? $"actions{pages.ToList().IndexOf(page) + 1}" : $"{query.ReturnType.Name}Actions";
    }

    /// <summary>
    /// Reads one page's menu, preserving item order and unique item names.
    /// </summary>
    /// <param name="page">The data page.</param>
    /// <param name="name">The toolbar name.</param>
    /// <returns>The toolbar, or null when no item is proven.</returns>
    ScreenToolbarModel? Read(JsxElement page, string name)
    {
        var items = new List<ScreenToolbarItemModel>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var menus = Menus(page).ToList();
        foreach (var menu in menus)
        {
            if (IsDirectMenu(page, menu))
            {
                ReadMenu(menu, page.Tag, items, names);
            }
            else
            {
                unread.MenuContainer(menu.Tag);
            }
        }

        foreach (var orphan in Items(page, page.Tag).Where(item => !menus.Exists(menu => Contains(menu, item))))
        {
            unread.OrphanMenuItem(orphan.Tag);
        }

        return items.Count == 0 ? null : new(name, items);
    }

    /// <summary>
    /// Finds both static page members and standalone imported menu containers.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <returns>The menus in source order.</returns>
    IEnumerable<JsxElement> Menus(JsxElement page) => components.MenuItems.Append($"{page.Tag}.MenuItems")
        .SelectMany(tag => JsxElements.Named(page, tag)).OrderBy(_ => _.Position);

    /// <summary>
    /// Finds both static page members and standalone imported item markers.
    /// </summary>
    /// <param name="parent">The containing element.</param>
    /// <param name="pageTag">The page's local tag.</param>
    /// <returns>The items in source order.</returns>
    IEnumerable<JsxElement> Items(JsxElement parent, string pageTag) => components.MenuItem.Append($"{pageTag}.MenuItem")
        .SelectMany(tag => JsxElements.Named(parent, tag)).OrderBy(_ => _.Position);

    /// <summary>
    /// Reads only menus with unconditional direct marker children.
    /// </summary>
    /// <param name="menu">The menu.</param>
    /// <param name="pageTag">The page tag.</param>
    /// <param name="items">The recovered items.</param>
    /// <param name="names">The used item names.</param>
    void ReadMenu(JsxElement menu, string pageTag, List<ScreenToolbarItemModel> items, HashSet<string> names)
    {
        var markers = Items(menu, pageTag).ToList();
        if (!HasOnlyDirectItems(menu, markers))
        {
            unread.MenuChildren(menu.Tag);
            return;
        }

        foreach (var item in markers.Select(ReadItem).OfType<ScreenToolbarItemModel>())
        {
            items.Add(item with { Name = ItemName(item.Command, names) });
        }
    }

    /// <summary>
    /// Requires the container itself to be a direct page child outside any JSX expression.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="menu">The menu container.</param>
    /// <returns>Whether its unconditional placement is proven.</returns>
    bool IsDirectMenu(JsxElement page, JsxElement menu)
    {
        var tag = source.Tags.Single(_ => _.Start == menu.Position);
        if (tag.Parent < 0 || source.Tags[tag.Parent].Start != page.Position)
        {
            return false;
        }

        return !Enumerable.Range(page.ChildrenStart, menu.Position - page.ChildrenStart).Any(_ => EnclosesMenu(_, menu.Position));
    }

    /// <summary>
    /// Detects an expression enclosing the menu, using matched boundaries rather than its textual content.
    /// </summary>
    /// <param name="open">The possible expression opening.</param>
    /// <param name="position">The menu position.</param>
    /// <returns>Whether the boundary encloses it.</returns>
    bool EnclosesMenu(int open, int position) => source.Code[open] == '{' && source.CloseOf(open) > position;

    /// <summary>
    /// Checks source tag boundaries rather than matching conditional JSX as if it were unconditional.
    /// </summary>
    /// <param name="menu">The menu.</param>
    /// <param name="markers">The known item markers.</param>
    /// <returns>Whether all children are direct markers without expressions.</returns>
    bool HasOnlyDirectItems(JsxElement menu, List<JsxElement> markers)
    {
        if (!menu.IsReadable)
        {
            return false;
        }

        var index = source.Tags.ToList().FindIndex(_ => _.Start == menu.Position);
        if (source.Tags[index].HasContent)
        {
            return false;
        }

        var children = source.Tags.Where(_ => _.Parent == index).ToList();
        return children.Count == markers.Count && children.TrueForAll(child => markers.Exists(_ => _.Position == child.Start));
    }

    /// <summary>
    /// Reads one provable opener, reporting precisely why any other item is omitted.
    /// </summary>
    /// <param name="item">The item marker.</param>
    /// <returns>The item, or null when unread.</returns>
    ScreenToolbarItemModel? ReadItem(JsxElement item)
    {
        if (!item.IsReadable)
        {
            unread.Component(item.Tag, "no toolbar item is written for it");
            return null;
        }

        if (IsDisabled(item))
        {
            return null;
        }

        var callback = item["command"];
        if (callback is null)
        {
            unread.MenuItem(item["label"]?.Literal, null);
            return null;
        }

        var visibility = callback.Literal is null ? _callbacks.Visibility(item) : null;
        if (visibility is null)
        {
            ReportCallback(item, callback);
            return null;
        }

        return ReadOpener(item, visibility);
    }

    /// <summary>
    /// Reports disabled or unrepresentable computed enablement without inventing an active item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Whether enablement prevents emitting the item.</returns>
    bool IsDisabled(JsxElement item)
    {
        var disabled = item["disabled"];
        if (disabled is null || IsBoolean(disabled, "false"))
        {
            return false;
        }

        if (IsBoolean(disabled, "true"))
        {
            unread.DisabledMenuItem(item["label"]?.Literal);
        }
        else
        {
            unread.ComputedMenuEnablement(item["label"]?.Literal, disabled.Written);
        }

        return true;
    }

    /// <summary>
    /// Resolves the unique visible dialog to a command of this slice.
    /// </summary>
    /// <param name="item">The menu item.</param>
    /// <param name="visibility">The state visibility binding.</param>
    /// <returns>The recovered toolbar item, or null.</returns>
    ScreenToolbarItemModel? ReadOpener(JsxElement item, string visibility)
    {
        var dialogs = components.CommandDialogs.SelectMany(tag => JsxElements.Named(source, tag))
            .Where(_ => _["visible"]?.Identifier == visibility).ToList();
        if (!HasUniqueBinding(item, dialogs))
        {
            unread.MenuItem(item["label"]?.Literal, item["command"]!.Written);
            return null;
        }

        var dialog = dialogs[0];
        var command = CommandOf(dialog);
        if (command is null)
        {
            unread.MenuDialogCommand(item["label"]?.Literal);
            return null;
        }

        _claimedDialogs.Add(dialog.Position);
        ReportPresentation(item);
        return new(command, command, item["label"]?.Literal);
    }

    /// <summary>
    /// Requires a unique dialog whose visibility shares the setter's lexical tuple.
    /// </summary>
    /// <param name="item">The opener.</param>
    /// <param name="dialogs">The dialogs with this visibility spelling.</param>
    /// <returns>Whether the binding is proven.</returns>
    bool HasUniqueBinding(JsxElement item, List<JsxElement> dialogs) => dialogs.Count == 1 && _callbacks.SharesVisibility(item, dialogs[0]);

    /// <summary>
    /// Resolves the same local proxy identity used for ordinary dialog actions.
    /// </summary>
    /// <param name="dialog">The dialog.</param>
    /// <returns>The command name, or null when unread.</returns>
    string? CommandOf(JsxElement dialog)
    {
        if (!dialog.IsReadable || dialog["command"]?.Identifier is not { } local)
        {
            return null;
        }

        var proxy = components.Proxy(local);
        return commands.FirstOrDefault(_ => _.Name == proxy)?.Name;
    }

    /// <summary>
    /// Reports metadata omitted from an otherwise readable toolbar item.
    /// </summary>
    /// <param name="item">The item.</param>
    void ReportPresentation(JsxElement item)
    {
        if (item["label"] is { Literal: null } label)
        {
            unread.MenuLabel(label.Written);
        }

        if (item["disableOnUnselected"] is { } selection && selection.Written != "false")
        {
            unread.MenuEnablement(item["label"]?.Literal, "disableOnUnselected");
        }
    }

    /// <summary>
    /// Distinguishes route navigation, screen dialog composition and unread callbacks.
    /// </summary>
    /// <param name="item">The menu item.</param>
    /// <param name="callback">The callback.</param>
    void ReportCallback(JsxElement item, JsxValue callback)
    {
        if (callback.Literal is not null)
        {
            unread.MenuItem(item["label"]?.Literal, callback.Written);
            return;
        }

        if (_callbacks.DialogScreen(item) is { } screen)
        {
            unread.MenuScreenDialog(screen);
            return;
        }

        if (_callbacks.Route(callback.Written) is { } route)
        {
            unread.MenuRoute(route);
            return;
        }

        unread.MenuItem(item["label"]?.Literal, callback.Written);
    }
}
