// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads the structure of a screen from the Cratis Components it uses, and reports what of it stays unread.
/// </summary>
/// <param name="files">The <see cref="IUserInterfaceFiles"/> the text of a component is asked of.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything not inferred is reported to.</param>
/// <remarks>
/// A known component states its structure through properties whose meaning is fixed by the component rather than by
/// the screen: a <c>DataPage</c>'s <c>title</c>, the <c>query</c> a data table is bound to and the <c>field</c> and
/// <c>header</c> of each <c>Column</c>, the <c>command</c> a <c>CommandDialog</c> runs. Those are read, and only when
/// they are written as text or as the name of a query or command the slice declares. A computed title, a column
/// without a field, a menu item running a callback and a query from another slice are each left out and reported on
/// their own, so what the document says about a screen is always something the screen says too.
/// <para>
/// Nothing in the Cratis Components states navigation - a details component is a pane on the same screen - so no
/// <c>navigate to</c> is ever written.
/// </para>
/// </remarks>
public partial class ScreenStructureReader(IUserInterfaceFiles files, ScreenplayDiagnostics diagnostics)
{
    enum ComponentKind
    {
        Page = 0,
        Table = 1,
        Dialog = 2
    }

    /// <summary>
    /// Reads the structure of one screen.
    /// </summary>
    /// <param name="namespace">The namespace of the slice the screen belongs to.</param>
    /// <param name="name">The name of the screen.</param>
    /// <param name="path">The path of the file realizing it, as the compilation spells it.</param>
    /// <param name="queries">The queries the slice declares, under the names it declares them.</param>
    /// <param name="commands">The commands the slice declares.</param>
    /// <returns>The <see cref="ScreenStructure"/>.</returns>
    public ScreenStructure Read(
        string @namespace,
        string name,
        string path,
        IReadOnlyCollection<QueryModel> queries,
        IReadOnlyCollection<CommandModel> commands)
    {
        var unread = new UnreadScreenStructure(diagnostics, @namespace, name);
        if (files.Contents(path) is not { } text)
        {
            unread.Screen();
            return ScreenStructure.None;
        }

        var components = KnownComponents.In(text);
        var code = ScreenImports.WithoutComments(text);
        var elements = components.DataPages.SelectMany(_ => JsxElements.Named(code, _).Select(element => (Kind: ComponentKind.Page, Element: element)))
            .Concat(components.DataTables.SelectMany(_ => JsxElements.Named(code, _).Select(element => (Kind: ComponentKind.Table, Element: element))))
            .Concat(components.CommandDialogs.SelectMany(_ => JsxElements.Named(code, _).Select(element => (Kind: ComponentKind.Dialog, Element: element))))
            .OrderBy(_ => _.Element.Position)
            .ToList();

        if (elements.Count == 0)
        {
            unread.Screen();
            return ScreenStructure.None;
        }

        var reading = new Reading(components, queries, commands, unread);
        foreach (var (kind, element) in elements)
        {
            reading.Read(kind, element);
        }

        return new(reading.Titles, reading.Tables, reading.Actions.Distinct(StringComparer.Ordinal).ToList());
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PropertyPathRegex();

    sealed class Reading(
        KnownComponents components,
        IReadOnlyCollection<QueryModel> queries,
        IReadOnlyCollection<CommandModel> commands,
        UnreadScreenStructure unread)
    {
        public List<string> Titles { get; } = [];

        public List<ScreenTableModel> Tables { get; } = [];

        public List<string> Actions { get; } = [];

        public void Read(ComponentKind kind, JsxElement element)
        {
            switch (kind)
            {
                case ComponentKind.Page:
                    Page(element);
                    break;
                case ComponentKind.Table:
                    Table(element);
                    break;
                default:
                    Dialog(element);
                    break;
            }
        }

        void Page(JsxElement page)
        {
            if (!page.IsReadable)
            {
                unread.Component(page.Tag, "neither its title, its table nor its menu items are written for it");
                return;
            }

            switch (page["title"])
            {
                case { Literal: { Length: > 0 } title }:
                    Titles.Add(title);
                    break;
                case { Literal: null } computed:
                    unread.Title(page.Tag, computed.Written);
                    break;
            }

            Table(page);

            foreach (var item in JsxElements.Named(page.Children, $"{page.Tag}.MenuItem"))
            {
                unread.MenuItem(item["label"]?.Literal);
            }

            if (page["detailsComponent"] is not null)
            {
                unread.Details(page.Tag);
            }
        }

        void Table(JsxElement table)
        {
            if (!table.IsReadable)
            {
                unread.Component(table.Tag, "no table is written for it");
                return;
            }

            var written = table["query"];
            var query = written?.Identifier is { } local && components.Proxy(local) is { } proxy
                ? queries.FirstOrDefault(_ => _.Name == proxy)
                : null;

            if (query is null)
            {
                unread.Query(table.Tag, written?.Written);
                return;
            }

            var readModel = query.ReturnType.Name;
            Tables.Add(new(readModel, [.. Columns(table.Children, readModel)]));
        }

        IEnumerable<ScreenColumnModel> Columns(string children, string readModel)
        {
            var columns = components.Columns
                .SelectMany(binding => JsxElements.Named(children, binding.Local).Select(element => (Binding: binding, Element: element)))
                .OrderBy(_ => _.Element.Position);

            foreach (var (binding, column) in columns)
            {
                if (!KnownComponents.IsCratis(binding))
                {
                    unread.Column(readModel, $"is imported from '{binding.Module}' rather than from Cratis Components");
                    continue;
                }

                if (!column.IsReadable)
                {
                    unread.Column(readModel, "has its properties spread in from an object or not written in a shape the generator reads");
                    continue;
                }

                if (column["field"]?.Literal is not { } field || !PropertyPathRegex().IsMatch(field))
                {
                    unread.Column(readModel, "does not name the property it shows as text");
                    continue;
                }

                var header = column["header"];
                if (header is { Literal: null })
                {
                    unread.Header(readModel, field, header.Written);
                }

                yield return new(field, header?.Literal is { Length: > 0 } label ? label : null);
            }
        }

        void Dialog(JsxElement dialog)
        {
            if (!dialog.IsReadable)
            {
                unread.Component(dialog.Tag, "no action is written for it");
                return;
            }

            var written = dialog["command"];
            var command = written?.Identifier is { } local && components.Proxy(local) is { } proxy
                ? commands.FirstOrDefault(_ => _.Name == proxy)
                : null;

            if (command is null)
            {
                unread.Command(dialog.Tag, written?.Written);
                return;
            }

            Actions.Add(command.Name);
        }
    }
}
