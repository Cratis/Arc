// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reports, one directive at a time, what of a screen's structure stays unread.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> to report to.</param>
/// <param name="namespace">The namespace of the slice the screen belongs to.</param>
/// <param name="screen">The name of the screen.</param>
/// <remarks>
/// Every report names the component it is about and what was left out because of it, so the gap between the screen
/// and its document can be closed one directive at a time rather than read as a blanket statement about the screen.
/// </remarks>
public class UnreadScreenStructure(ScreenplayDiagnostics diagnostics, string @namespace, string screen)
{
    /// <summary>
    /// Reports a screen none of whose structure is read.
    /// </summary>
    public void Screen() => Report(
        "uses none of the Cratis Components the generator recognizes - DataPage, DataTableForQuery, DataTableForObservableQuery and CommandDialog - so beyond the file realizing it and the queries it binds, its title, sections, tables, summaries, actions and navigation are not inferred");

    /// <summary>
    /// Reports a component whose properties could not be read.
    /// </summary>
    /// <param name="tag">The tag of the component.</param>
    /// <param name="consequence">What is left out because of it.</param>
    public void Component(string tag, string consequence) =>
        Report($"uses a <{tag}> whose properties are spread in from an object or not written in a shape the generator reads, so {consequence}");

    /// <summary>
    /// Reports a data page whose title is computed.
    /// </summary>
    /// <param name="tag">The tag of the data page.</param>
    /// <param name="written">The title as written.</param>
    public void Title(string tag, string written) =>
        Report($"titles a <{tag}> with '{written}', which is computed rather than written as text, so no title is written for it");

    /// <summary>
    /// Reports a table bound to something that is not a query of its slice.
    /// </summary>
    /// <param name="tag">The tag of the table.</param>
    /// <param name="written">The query as written, or null when it states none.</param>
    public void Query(string tag, string? written) =>
        Report(written is null
            ? $"uses a <{tag}> without a query, so no table is written for it"
            : $"binds a <{tag}> to '{written}', which is not a query its slice declares, so no table is written for it");

    /// <summary>
    /// Reports a column that is left out.
    /// </summary>
    /// <param name="readModel">The read model of the table the column belongs to.</param>
    /// <param name="why">Why it is left out.</param>
    public void Column(string readModel, string why) =>
        Report($"has a column in the table of '{readModel}' that {why}, so the column is left out");

    /// <summary>
    /// Reports a column whose header is computed.
    /// </summary>
    /// <param name="readModel">The read model of the table the column belongs to.</param>
    /// <param name="property">The property the column shows.</param>
    /// <param name="written">The header as written.</param>
    public void Header(string readModel, string property, string written) =>
        Report($"heads the column '{property}' in the table of '{readModel}' with '{written}', which is not written as text, so the column is written without a label");

    /// <summary>
    /// Reports a menu item whose command is absent or unread.
    /// </summary>
    /// <param name="label">The literal label, when present.</param>
    /// <param name="written">The callback, or null when absent.</param>
    public void MenuItem(string? label, string? written) =>
        Report(written is null
            ? $"has a menu item {ItemLabel(label)} that does nothing when clicked, so no item is written"
            : $"has a menu item {ItemLabel(label)} that runs a callback whose command '{written}' is not read, so no toolbar item is written");

    /// <summary>
    /// Reports conditional or mixed menu children.
    /// </summary>
    /// <param name="tag">The menu tag.</param>
    public void MenuChildren(string tag) => Report($"uses a <{tag}> with conditional or mixed children rather than unconditional direct MenuItem markers, so no toolbar items are written for it");

    /// <summary>
    /// Reports a literal disabled item.
    /// </summary>
    /// <param name="label">The literal label.</param>
    public void DisabledMenuItem(string? label) => Report($"has a menu item {ItemLabel(label)} that is disabled, so no active toolbar item is written");

    /// <summary>
    /// Reports a menu opener whose dialog command stays unread.
    /// </summary>
    /// <param name="label">The literal label.</param>
    public void MenuDialogCommand(string? label) => Report($"has a menu item {ItemLabel(label)} opening a dialog whose dialog's command is not read, so no toolbar item is written");

    /// <summary>
    /// Reports a computed item label without dropping its proven action.
    /// </summary>
    /// <param name="written">The computed label.</param>
    public void MenuLabel(string written) => Report($"labels a menu item with '{written}', which is computed rather than written as text, so the item is written without a label");

    /// <summary>
    /// Reports a route without inventing its screen destination or parameter bindings.
    /// </summary>
    /// <param name="route">The literal route.</param>
    public void MenuRoute(string route) => Report($"has a menu item that navigates to route '{route}'; which screen a route shows is not read, so no navigate to or parameter binding is written");

    /// <summary>
    /// Reports a screen opened as a dialog rather than a dialog template.
    /// </summary>
    /// <param name="screenName">The component name.</param>
    public void MenuScreenDialog(string screenName) => Composition($"opens {screenName} as a dialog; a toolbar item can only open a dialog template, so no item is written");

    /// <summary>
    /// Reports enablement the toolbar grammar cannot state.
    /// </summary>
    /// <param name="label">The literal label.</param>
    /// <param name="written">The enablement expression or property.</param>
    public void MenuEnablement(string? label, string written) => Composition($"has a menu item {ItemLabel(label)} with '{written}' enablement; Screenplay has no toolbar counterpart, so its enablement is not stated");

    /// <summary>
    /// Reports behavior occurring after command success, distinct from opening command input.
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <param name="written">The callback.</param>
    public void AfterSuccess(string command, string written) => Report($"runs '{written}' after success, which is not read, so no navigate to or follow-up is written for action {command}");

    /// <summary>
    /// Reports a data page showing a details component.
    /// </summary>
    /// <param name="tag">The tag of the data page.</param>
    /// <param name="written">The details component as written.</param>
    public void Details(string tag, string written) =>
        Composition($"shows a details component beside the <{tag}>, a pane on the same screen showing '{written}' for the selected row; Screenplay has no same-screen composition for it, so neither navigation nor its content is written. A slice-directory details file remains a separate screen");

    /// <summary>
    /// Reports a command dialog running something that is not a command of its slice.
    /// </summary>
    /// <param name="tag">The tag of the dialog.</param>
    /// <param name="written">The command as written, or null when it states none.</param>
    public void Command(string tag, string? written) =>
        Report(written is null
            ? $"uses a <{tag}> without a command, so no action is written for it"
            : $"runs '{written}' from a <{tag}>, which is not a command its slice declares, so no action is written for it");

    /// <summary>
    /// Reports a scan boundary that prevents trustworthy structure inference.
    /// </summary>
    /// <param name="line">The line at which the boundary starts.</param>
    /// <param name="construct">The construct that cannot be scanned.</param>
    internal void Unscannable(int line, string construct) =>
        Report($"cannot be scanned past line {line} ({construct}), so beyond the file realizing it and the queries it imports, its structure is not inferred");

    /// <summary>
    /// Reports an unaccounted component tag once.
    /// </summary>
    /// <param name="tag">The tag.</param>
    internal void Element(string tag) =>
        Report($"renders a <{tag}> beside the Cratis Components it reads, and what a <{tag}> shows is not read, so any title, section, summary, action or navigation it carries is not written");

    /// <summary>
    /// Reports the unaccounted intrinsic tags together.
    /// </summary>
    /// <param name="tags">The distinct tags in source order.</param>
    internal void Markup(IEnumerable<string> tags) =>
        Report($"renders {string.Join(", ", tags.Select(_ => $"<{_}>"))} beside the Cratis Components it reads, and what that markup shows is not read, so any title, section, summary, action or navigation it carries is not written");

    /// <summary>
    /// Describes an item by its literal label.
    /// </summary>
    /// <param name="label">The literal label.</param>
    /// <returns>The diagnostic label.</returns>
    static string ItemLabel(string? label) => label is null ? "without a label written as text" : $"'{label}'";

    /// <summary>
    /// Reports a composition contract missing from Screenplay.
    /// </summary>
    /// <param name="what">The omitted composition.</param>
    void Composition(string what) =>
        diagnostics.Information(ScreenplayDiagnosticCodes.ScreenCompositionNotRepresentable, $"The screen '{screen}' {what}", @namespace);

    /// <summary>
    /// Reports structure the generator cannot infer.
    /// </summary>
    /// <param name="what">The omitted structure.</param>
    void Report(string what) =>
        diagnostics.Information(ScreenplayDiagnosticCodes.ScreenStructureNotInferred, $"The screen '{screen}' {what}", @namespace);
}
