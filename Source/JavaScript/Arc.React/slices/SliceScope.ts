// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';

/**
 * Defines how a {@link SliceScope} decides its slice location.
 */
export enum SliceScopeKind {
    /**
     * Resolves from the slice locations registered within it, falling back to the parent scope.
     */
    boundary = 'boundary',

    /**
     * Starts from the location of the folder the rendering component lives in, refined by the slice locations
     * registered within it when they lie on the same path.
     */
    folder = 'folder',

    /**
     * Always resolves to the location it was given, for itself and every scope nested within it that is not explicit
     * itself; registrations and folder locations within it are ignored.
     */
    explicit = 'explicit'
}

/**
 * Represents a node in the tree of slice scopes that resolves which module, feature and slice the components
 * rendered within it belong to.
 * @remarks
 * Command and query hooks register the slice location of their proxy on the nearest scope. The scope is an external
 * store for `useSyncExternalStore`: registrations notify subscribers, and the snapshot only changes identity when the
 * resolved location changes, so reading the context never causes a render loop.
 */
export class SliceScope {
    private readonly _registrations = new Map<number, SliceLocation>();
    private readonly _listeners = new Set<() => void>();
    private _nextRegistration = 0;
    private _unsubscribeFromParent?: () => void;
    private _snapshot: SliceLocation = SliceLocation.empty;

    /**
     * Initializes a new instance of the {@link SliceScope} class.
     * @param parent The scope this scope is nested in, if any.
     * @param declared The location declared for the scope, used by the {@link SliceScopeKind.folder} and
     * {@link SliceScopeKind.explicit} kinds.
     * @param kind How the scope decides its slice location.
     */
    constructor(
        readonly parent?: SliceScope,
        readonly declared: SliceLocation = SliceLocation.empty,
        readonly kind: SliceScopeKind = SliceScopeKind.boundary) {
    }

    /**
     * Registers a slice location in use within the scope, typically the location of a command or query proxy.
     * @param location The slice location to register. Empty locations are ignored.
     * @returns A callback that removes the registration.
     */
    register(location: SliceLocation): () => void {
        if (location.isEmpty) {
            return nothingToUnregister;
        }

        const registration = this._nextRegistration++;
        this._registrations.set(registration, location);
        this.notify();

        return () => {
            if (this._registrations.delete(registration)) {
                this.notify();
            }
        };
    }

    /**
     * Subscribes to changes that may affect the resolved location of the scope.
     * @param listener The callback to call on a change.
     * @returns A callback that removes the subscription.
     */
    readonly subscribe = (listener: () => void): (() => void) => {
        this._listeners.add(listener);
        if (this._listeners.size === 1 && this.parent) {
            this._unsubscribeFromParent = this.parent.subscribe(this.notify);
        }

        return () => {
            this._listeners.delete(listener);
            if (this._listeners.size === 0) {
                this._unsubscribeFromParent?.();
                this._unsubscribeFromParent = undefined;
            }
        };
    };

    /**
     * Gets the resolved location, keeping the same instance for as long as the location does not change.
     * @returns The resolved {@link SliceLocation}; {@link SliceLocation.empty} when nothing is known.
     */
    readonly getSnapshot = (): SliceLocation => {
        const resolved = this.resolve();
        if (!resolved.equals(this._snapshot)) {
            this._snapshot = resolved;
        }
        return this._snapshot;
    };

    /**
     * Resolves the location of the scope from what is declared, what is registered within it and its parent.
     * @returns The resolved {@link SliceLocation}.
     */
    resolve(): SliceLocation {
        const explicit = this.nearestExplicit();
        if (explicit) {
            return explicit.declared;
        }

        const registered = commonLocationOf([...this._registrations.values()]);

        if (this.kind === SliceScopeKind.folder && !this.declared.isEmpty) {
            return registered && liesOnPath(registered, this.declared) ? registered : this.declared;
        }

        return registered ?? this.parent?.getSnapshot() ?? SliceLocation.empty;
    }

    private nearestExplicit(): SliceScope | undefined {
        return this.kind === SliceScopeKind.explicit ? this : this.parent?.nearestExplicit();
    }

    private readonly notify = () => {
        this._listeners.forEach(listener => listener());
    };
}

function nothingToUnregister(): void {
    // An empty location is never registered, so there is nothing to remove.
}

/**
 * Gets the parts all locations agree on, from the module down: once a part differs, it and the parts below it are
 * left empty.
 * @param locations The locations to combine.
 * @returns The combined location, or undefined when there are none or they agree on nothing.
 */
function commonLocationOf(locations: SliceLocation[]): SliceLocation | undefined {
    if (locations.length === 0) {
        return undefined;
    }

    const [first, ...rest] = locations;
    const sameModule = rest.every(location => location.module === first.module);
    const sameFeature = sameModule && rest.every(location => location.feature === first.feature);
    const sameSlice = sameFeature && rest.every(location => location.slice === first.slice);

    const common = new SliceLocation(
        sameModule ? first.module : '',
        sameFeature ? first.feature : '',
        sameSlice ? first.slice : '');

    return common.isEmpty ? undefined : common;
}

/**
 * Checks whether a location is the same as, or nested below, another when both are read as paths.
 * @param location The location to check.
 * @param path The location it must lie on.
 * @returns True when it lies on the path, false otherwise.
 */
function liesOnPath(location: SliceLocation, path: SliceLocation): boolean {
    const locationPath = location.toString();
    const pathAsString = path.toString();
    return locationPath === pathAsString || locationPath.startsWith(`${pathAsString}/`);
}
