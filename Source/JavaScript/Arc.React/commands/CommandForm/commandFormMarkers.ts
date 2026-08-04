// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The `displayName` a command form field carries.
 * @remarks
 * Exported so a consumer recognising a field has a constant to compare against rather than a duplicated string
 * literal, and so the two values below have one definition between them.
 */
export const CommandFormFieldDisplayName = 'CommandFormField';

/**
 * The `displayName` a command form column carries.
 */
export const CommandFormColumnDisplayName = 'CommandFormColumn';

/**
 * The shape a component carries to say what it is to a {@link CommandForm}.
 */
export type CommandFormMarked = {
    /** Set on a field component. */
    isCommandFormField?: boolean;

    /** Set on a column component. */
    isCommandFormColumn?: boolean;

    /** The React display name, kept as the compatibility fallback. */
    displayName?: string;
};

/**
 * Marks a component as a command form field.
 * @param component The component to mark.
 * @returns The same component, typed as marked.
 * @remarks
 * Sets both the marker and the `displayName`. The `displayName` is not redundant and is not on a deprecation path:
 * it is what lets a version of this package interoperate with a version of a consuming package that only knows the
 * string, in both directions. Removing it would silently unbind every field across that version boundary - the very
 * failure the marker exists to prevent.
 */
export function markAsCommandFormField<T>(component: T): T & CommandFormMarked {
    const marked = component as T & CommandFormMarked;
    marked.isCommandFormField = true;
    marked.displayName = CommandFormFieldDisplayName;
    return marked;
}

/**
 * Marks a component as a command form column.
 * @param component The component to mark.
 * @returns The same component, typed as marked.
 */
export function markAsCommandFormColumn<T>(component: T): T & CommandFormMarked {
    const marked = component as T & CommandFormMarked;
    marked.isCommandFormColumn = true;
    marked.displayName = CommandFormColumnDisplayName;
    return marked;
}

/**
 * Whether a component is a command form field.
 * @param component The component to check.
 * @returns True when it is.
 * @remarks
 * The marker is checked first and the `displayName` second. A build transform that rewrites `displayName` - which
 * `react-docgen-typescript` does by default, and Storybook selects it through a documented option - leaves the
 * marker alone, so a field survives where it used to unbind silently. The fallback keeps a hand-marked component,
 * and a component from a package that predates the marker, working exactly as before.
 */
export function isCommandFormField(component: CommandFormMarked | undefined): boolean {
    return component?.isCommandFormField === true || component?.displayName === CommandFormFieldDisplayName;
}

/**
 * Whether a component is a command form column.
 * @param component The component to check.
 * @returns True when it is.
 */
export function isCommandFormColumn(component: CommandFormMarked | undefined): boolean {
    return component?.isCommandFormColumn === true || component?.displayName === CommandFormColumnDisplayName;
}
