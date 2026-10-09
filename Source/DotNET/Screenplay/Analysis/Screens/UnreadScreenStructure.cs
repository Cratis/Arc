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
    /// Reports a menu item whose action is a callback.
    /// </summary>
    /// <param name="label">The label of the menu item, when it is written as text.</param>
    public void MenuItem(string? label) =>
        Report($"has a menu item {(label is null ? "without a label written as text" : $"'{label}'")} that runs a callback, and what a callback does is not read, so no action is written for it");

    /// <summary>
    /// Reports a data page showing a details component.
    /// </summary>
    /// <param name="tag">The tag of the data page.</param>
    public void Details(string tag) =>
        Report($"shows a details component beside the <{tag}>, which is a pane on the same screen rather than a screen it navigates to, so neither a navigation nor its content is written for it");

    /// <summary>
    /// Reports a command dialog running something that is not a command of its slice.
    /// </summary>
    /// <param name="tag">The tag of the dialog.</param>
    /// <param name="written">The command as written, or null when it states none.</param>
    public void Command(string tag, string? written) =>
        Report(written is null
            ? $"uses a <{tag}> without a command, so no action is written for it"
            : $"runs '{written}' from a <{tag}>, which is not a command its slice declares, so no action is written for it");

    void Report(string what) =>
        diagnostics.Information(ScreenplayDiagnosticCodes.ScreenStructureNotInferred, $"The screen '{screen}' {what}", @namespace);
}
