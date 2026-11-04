// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../slices/SliceScope.js';
import { commonLocationOf } from '../slices/commonLocationOf.js';
import { ApplicationScreen, emptyApplicationScreen } from './ApplicationScreen.js';
import { ApplicationDialog } from './ApplicationDialog.js';
import { ApplicationContextSnapshot } from './ApplicationContextSnapshot.js';
import { ApplicationDialogRegistration } from './ApplicationDialogRegistration.js';
import { ApplicationContextContribution, ApplicationContextContributionHandle } from './ApplicationContextContribution.js';

interface RegisteredScope {
    scope: SliceScope;
    layer: string | undefined;
    unsubscribe: () => void;
}

interface RegisteredDialog {
    id: string;
    title?: string;
}

/**
 * The external store holding what the user is looking at: the focused location, the active slices, the screen, the open
 * dialogs and host contributions.
 * @remarks
 * `subscribe` and `getSnapshot` are arrow properties, so they can be handed straight to `useSyncExternalStore` or used
 * from code outside React. Slice scopes register themselves in a layer: the page layer (no layer id) or the layer of
 * an open dialog. The top-most open dialog is focused, otherwise the page.
 */
export class ApplicationContext {
    private readonly _scopes = new Set<RegisteredScope>();
    private readonly _dialogs: RegisteredDialog[] = [];
    private readonly _contributions: ApplicationContextContribution[] = [];
    private readonly _listeners = new Set<() => void>();
    private _snapshot: ApplicationContextSnapshot = emptySnapshot();
    private _dirty = true;

    /**
     * Subscribes to changes of the snapshot.
     * @param listener The callback to call on a change.
     * @returns A callback that removes the subscription.
     */
    readonly subscribe = (listener: () => void): (() => void) => {
        this._listeners.add(listener);
        return () => {
            this._listeners.delete(listener);
        };
    };

    /**
     * Gets the current snapshot, keeping the same instance for as long as nothing in it changes.
     * @returns The {@link ApplicationContextSnapshot}.
     */
    readonly getSnapshot = (): ApplicationContextSnapshot => {
        if (this._dirty) {
            this._dirty = false;
            const computed = this.compute();
            if (!sameSnapshot(this._snapshot, computed)) {
                this._snapshot = computed;
            }
        }
        return this._snapshot;
    };

    /**
     * Registers a slice scope as part of a layer.
     * @param scope The scope to register.
     * @param layer The id of the dialog the scope belongs to; undefined for the page.
     * @returns A callback that removes the registration.
     */
    registerScope(scope: SliceScope, layer?: string): () => void {
        const entry = this.addScope(scope, layer);
        this.invalidate();

        return () => {
            if (this.removeScope(entry)) {
                this.invalidate();
            }
        };
    }

    /**
     * Registers an open dialog and the scope it renders in. The dialog is pushed on top of the stack and gets focus.
     * @param dialog The dialog id, optional title and the scope its content renders in.
     * @returns A callback that removes the dialog.
     */
    registerDialog(dialog: { id: string, title?: string, scope: SliceScope }): () => void {
        const registration = this.trackDialog(dialog);
        return () => registration.remove();
    }

    /**
     * Registers an open dialog and the scope it renders in. The dialog is pushed on top of the stack and gets focus.
     * @param dialog The dialog id, optional title and the scope its content renders in.
     * @returns A handle for updating and removing the dialog.
     * @remarks
     * Updating the handle changes the dialog in place and never its place in the stack, so the order of the dialogs
     * and the focus stay with the top-most dialog. Built-in dialogs register through this method, so an override of
     * `registerDialog` or `registerScope` does not see them.
     */
    trackDialog(dialog: { id: string, title?: string, scope: SliceScope }): ApplicationDialogRegistration {
        const entry: RegisteredDialog = { id: dialog.id, title: dialog.title };
        this._dialogs.push(entry);
        const scopeEntry = this.addScope(dialog.scope, dialog.id);
        let removed = false;
        this.invalidate();

        return {
            update: (next: { title?: string, scope: SliceScope }) => {
                if (removed) {
                    return;
                }
                let changed = false;
                if (entry.title !== next.title) {
                    entry.title = next.title;
                    changed = true;
                }
                if (scopeEntry.scope !== next.scope) {
                    scopeEntry.unsubscribe();
                    scopeEntry.scope = next.scope;
                    scopeEntry.unsubscribe = next.scope.subscribe(this.invalidate);
                    changed = true;
                }
                if (changed) {
                    this.invalidate();
                }
            },
            remove: () => {
                if (removed) {
                    return;
                }
                removed = true;
                const index = this._dialogs.indexOf(entry);
                if (index >= 0) {
                    this._dialogs.splice(index, 1);
                }
                this.removeScope(scopeEntry);
                this.invalidate();
            }
        };
    }

    /**
     * Contributes the screen and host values.
     * @param contribution What to contribute. Later contributions win over earlier ones.
     * @returns A handle for updating and removing the contribution.
     */
    contribute(contribution: ApplicationContextContribution): ApplicationContextContributionHandle {
        let current = contribution;
        this._contributions.push(current);
        this.invalidate();

        return {
            update: (next: ApplicationContextContribution) => {
                const index = this._contributions.indexOf(current);
                if (index < 0) {
                    return;
                }
                this._contributions[index] = next;
                current = next;
                this.invalidate();
            },
            remove: () => {
                const index = this._contributions.indexOf(current);
                if (index >= 0) {
                    this._contributions.splice(index, 1);
                    this.invalidate();
                }
            }
        };
    }

    private addScope(scope: SliceScope, layer: string | undefined): RegisteredScope {
        const entry: RegisteredScope = { scope, layer, unsubscribe: scope.subscribe(this.invalidate) };
        this._scopes.add(entry);
        return entry;
    }

    private removeScope(entry: RegisteredScope): boolean {
        const removed = this._scopes.delete(entry);
        if (removed) {
            entry.unsubscribe();
        }
        return removed;
    }

    private readonly invalidate = () => {
        this._dirty = true;
        this._listeners.forEach(listener => listener());
    };

    private compute(): ApplicationContextSnapshot {
        const dialogs: ApplicationDialog[] = this._dialogs.map(dialog => ({
            id: dialog.id,
            title: dialog.title,
            location: this.locationOfLayer(dialog.id)
        }));

        const focusedLayer = this._dialogs.length > 0 ? this._dialogs[this._dialogs.length - 1].id : undefined;
        const focused = this.scopesOfLayer(focusedLayer);

        return Object.freeze({
            location: this.locationOfLayer(focusedLayer),
            activeSlices: Object.freeze(distinct(focused.flatMap(scope => scope.registered))),
            screen: this.computeScreen(),
            dialogs: Object.freeze(dialogs),
            contributions: Object.freeze(Object.assign({}, ...this._contributions.map(c => c.values ?? {})))
        });
    }

    private computeScreen(): ApplicationScreen {
        let id = '';
        let name = '';
        let path = '';
        for (const contribution of this._contributions) {
            id = contribution.screen?.id ?? id;
            name = contribution.screen?.name ?? name;
            path = contribution.screen?.path ?? path;
        }
        return id === '' && name === '' && path === '' ? emptyApplicationScreen : Object.freeze({ id, name, path });
    }

    private scopesOfLayer(layer: string | undefined): SliceScope[] {
        return [...this._scopes].filter(entry => entry.layer === layer).map(entry => entry.scope);
    }

    private locationOfLayer(layer: string | undefined): SliceLocation {
        const scopes = this.scopesOfLayer(layer);
        const leaves = scopes.filter(scope => !scopes.some(other => other !== scope && isAncestor(scope, other)));
        return commonLocationOf(leaves.map(leaf => leaf.getSnapshot()).filter(location => !location.isEmpty)) ?? SliceLocation.empty;
    }
}

function isAncestor(candidate: SliceScope, scope: SliceScope): boolean {
    for (let current = scope.parent; current; current = current.parent) {
        if (current === candidate) {
            return true;
        }
    }
    return false;
}

function distinct(locations: SliceLocation[]): SliceLocation[] {
    const result: SliceLocation[] = [];
    for (const location of locations) {
        if (!result.some(existing => existing.equals(location))) {
            result.push(location);
        }
    }
    return result;
}

function emptySnapshot(): ApplicationContextSnapshot {
    return Object.freeze({
        location: SliceLocation.empty,
        activeSlices: Object.freeze([]),
        screen: emptyApplicationScreen,
        dialogs: Object.freeze([]),
        contributions: Object.freeze({})
    });
}

function sameSnapshot(a: ApplicationContextSnapshot, b: ApplicationContextSnapshot): boolean {
    return a.location.equals(b.location)
        && a.screen.id === b.screen.id && a.screen.name === b.screen.name && a.screen.path === b.screen.path
        && a.activeSlices.length === b.activeSlices.length
        && a.activeSlices.every((location, index) => location.equals(b.activeSlices[index]))
        && a.dialogs.length === b.dialogs.length
        && a.dialogs.every((dialog, index) => dialog.id === b.dialogs[index].id
            && dialog.title === b.dialogs[index].title
            && dialog.location.equals(b.dialogs[index].location))
        && sameValues(a.contributions, b.contributions);
}

function sameValues(a: Readonly<Record<string, unknown>>, b: Readonly<Record<string, unknown>>): boolean {
    const keys = Object.keys(a);
    return keys.length === Object.keys(b).length && keys.every(key => key in b && Object.is(a[key], b[key]));
}
