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
/// they are written as text or as an imported query or command resolved to its declaring slice. A computed title,
/// a column without a field, an unread menu callback and an unresolved proxy binding are left out and reported on
/// their own, so what the document says about a screen is always something the screen says too.
/// <para>
/// A literal router gesture is emitted only when an application route table proves which screen it shows.
/// A details component is a pane on the same screen, not navigation.
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
    /// <param name="references">The application-wide proxy resolver, when available.</param>
    /// <param name="navigation">The literal route resolver, when a route table is available.</param>
    /// <returns>The <see cref="ScreenStructure"/>.</returns>
    public ScreenStructure Read(
        string @namespace,
        string name,
        string path,
        IReadOnlyCollection<QueryModel> queries,
        IReadOnlyCollection<CommandModel> commands,
        ScreenProxyReferences? references = null,
        Func<string, ScreenNavigationModel?>? navigation = null)
    {
        var unread = new UnreadScreenStructure(diagnostics, @namespace, name);
        if (files.Contents(path) is not { } text)
        {
            unread.Screen();
            return ScreenStructure.None;
        }

        var code = TsxSource.Scan(text);
        var components = new KnownComponents(ScreenImports.Bindings(code));
        if (!HasKnownComponents(components))
        {
            unread.Screen();
            return ScreenStructure.None;
        }

        if (code.Problem is { } problem)
        {
            unread.Unscannable(problem.Line, problem.Construct);
            return ScreenStructure.None;
        }
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

        references ??= new(@namespace, path, components, queries, commands);
        var menus = new MenuItemReader(code, components, references, unread, navigation);
        var success = new ScreenSuccessNavigation(new MenuCallbacks(code, components), navigation, unread);
        var toolbars = menus.ReadPages(elements.Where(_ => _.Kind == ComponentKind.Page).Select(_ => _.Element).ToList());
        var reading = new Reading(components, references, unread, success) { Menus = menus };
        foreach (var (kind, element) in elements)
        {
            reading.Read(kind, element);
        }

        ReportUnaccounted(code, elements.ConvertAll(_ => _.Element), unread);
        UnreadNavigationGestures.Report(code, components, new MenuCallbacks(code, components), unread);

        var afterSuccess = success.Resolved();
        return new(reading.Titles, reading.Tables, reading.Actions.Concat(afterSuccess.Keys).Distinct(StringComparer.Ordinal).ToList())
        {
            Toolbars = toolbars,
            AfterSuccess = afterSuccess
        };
    }

    static bool HasKnownComponents(KnownComponents components) =>
        components.DataPages.Concat(components.DataTables).Concat(components.CommandDialogs).Any();

    static void ReportUnaccounted(TsxSource source, IReadOnlyList<JsxElement> read, UnreadScreenStructure unread)
    {
        var outside = source.Tags.Where(tag => IsUnaccounted(tag, read)).ToList();
        foreach (var tag in outside.Where(_ => !IsMarkup(_)).Select(_ => _.Name).Distinct(StringComparer.Ordinal))
        {
            unread.Element(tag);
        }

        var markup = outside.Where(IsMarkup).Select(_ => _.Name).Distinct(StringComparer.Ordinal).ToList();
        if (markup.Count > 0)
        {
            unread.Markup(markup);
        }
    }

    static bool IsUnaccounted(JsxTag tag, IReadOnlyList<JsxElement> read) => !IsAccounted(tag, read) && !IsExempt(tag, read);

    static bool IsAccounted(JsxTag tag, IReadOnlyList<JsxElement> read) => read.Any(_ => Contains(_, tag.Start));

    static bool Contains(JsxElement element, int position) => position >= element.Position && position < element.End;

    static bool IsMarkup(JsxTag tag) => tag.IsIntrinsic || tag.IsFragment;

    static bool IsExempt(JsxTag tag, IReadOnlyList<JsxElement> read)
    {
        if (!IsMarkup(tag) || tag.HasContent)
        {
            return false;
        }

        return tag.IsFragment || read.Any(_ => IsWithin(tag, _));
    }

    static bool IsWithin(JsxTag tag, JsxElement element) => element.Position > tag.Start && element.End <= tag.End;

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PropertyPathRegex();

    sealed class Reading(
        KnownComponents components,
        ScreenProxyReferences references,
        UnreadScreenStructure unread,
        ScreenSuccessNavigation success)
    {
        /// <summary>
        /// Gets the menu reader that tracks claimed dialogs.
        /// </summary>
        public MenuItemReader? Menus { get; init; }

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

            if (page["detailsComponent"] is { } details)
            {
                unread.Details(page.Tag, details.Written);
            }
        }

        void Table(JsxElement table) => ReadBound(
            table,
            ComponentKind.Table,
            references.Query,
            query => AddTable(table, query));

        void AddTable(JsxElement table, ScreenDataModel query)
        {
            var readModel = query.Type.Name;
            Tables.Add(new(readModel, [.. Columns(table, readModel)]));
        }

        IEnumerable<ScreenColumnModel> Columns(JsxElement parent, string readModel)
        {
            var columns = components.Columns
                .SelectMany(binding => JsxElements.Named(parent, binding.Local).Select(element => (Binding: binding, Element: element)))
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

                if (column["body"] is not null || column["selectionMode"] is not null)
                {
                    unread.Column(readModel, "renders its cells with a callback or as a selection control rather than showing a property");
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

        void Dialog(JsxElement dialog) => ReadBound(
            dialog,
            ComponentKind.Dialog,
            references.Command,
            command => ReadDialogCommand(dialog, command));

        /// <summary>
        /// Writes unclaimed dialog actions and keeps post-success behavior separate from input opening.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="command">The command name.</param>
        void ReadDialogCommand(JsxElement dialog, string command)
        {
            if (Menus?.Claims(dialog) != true)
            {
                Actions.Add(command);
            }

            success.Read(dialog, command);
        }

        void ReadBound<T>(JsxElement element, ComponentKind kind, Func<string, T?> resolve, Action<T> read)
            where T : class
        {
            var binding = BindingOf(kind);
            if (!element.IsReadable)
            {
                unread.Component(element.Tag, binding.Consequence);
                return;
            }

            var written = element[binding.Property];
            var declaration = written?.Identifier is { } local
                ? resolve(local)
                : null;

            if (declaration is null)
            {
                binding.Report(element.Tag, written?.Written);
                return;
            }

            read(declaration);
        }

        (string Property, string Consequence, Action<string, string?> Report) BindingOf(ComponentKind kind) =>
            kind == ComponentKind.Table
                ? ("query", "no table is written for it", unread.Query)
                : ("command", "no action is written for it", unread.Command);
    }
}
