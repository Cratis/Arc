// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComponentType, ReactNode, Ref, createElement, forwardRef, memo, useContext, useMemo } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from './SliceScope.js';
import { SliceScopeContext } from './SliceScopeContext.js';

interface FolderSliceProps {
    location: SliceLocation;
    children?: ReactNode;
}

const FolderSlice = (props: FolderSliceProps) => {
    const parent = useContext(SliceScopeContext);
    const { module, feature, slice } = props.location;

    const scope = useMemo(
        () => new SliceScope(parent, new SliceLocation(module, feature, slice), SliceScopeKind.folder),
        [parent, module, feature, slice]);

    return (
        <SliceScopeContext.Provider value={scope}>
            {props.children}
        </SliceScopeContext.Provider>
    );
};
FolderSlice.displayName = 'FolderSlice';

const memoType = Symbol.for('react.memo');
const forwardRefType = Symbol.for('react.forward_ref');
const lazyType = Symbol.for('react.lazy');

interface ReactTypeObject {
    $$typeof?: symbol;
    type?: unknown;
    compare?: (previous: object, next: object) => boolean;
    displayName?: string;
    name?: string;
}

type AnyProps = Record<string, unknown>;

const typeOf = (component: unknown): symbol | undefined =>
    (typeof component === 'object' || typeof component === 'function') && component !== null
        ? (component as ReactTypeObject).$$typeof
        : undefined;

const isClassComponent = (component: unknown) =>
    typeof component === 'function' && Boolean((component as { prototype?: { isReactComponent?: unknown } }).prototype?.isReactComponent);

/**
 * Checks whether a value is something React can render as a component type.
 * @param value The value to check.
 * @returns True for function and class components and for memo, forwardRef and lazy components.
 */
function isComponentType(value: unknown): boolean {
    if (typeof value === 'function') {
        return true;
    }
    const type = typeOf(value);
    return type === memoType || type === forwardRefType || type === lazyType;
}

function nameOf(component: unknown): string | undefined {
    const typed = component as ReactTypeObject;
    if (typed.displayName) {
        return typed.displayName;
    }
    if (typeof component === 'function' && component.name) {
        return component.name;
    }
    if (typeOf(component) === forwardRefType) {
        return nameOf((component as { render?: unknown }).render);
    }
    return typeOf(component) === memoType ? nameOf(typed.type) : undefined;
}

function named<T extends object>(target: T, name: string | undefined): T {
    if (name) {
        (target as ReactTypeObject).displayName = name;
        if (typeof target === 'function') {
            Object.defineProperty(target, 'name', { value: name, configurable: true });
        }
    }
    return target;
}

/**
 * Makes a component render within a slice scope for the slice folder it lives in.
 * @remarks
 * The Arc Vite slice location plugin wraps exported components in slice folders with this, so the component itself,
 * and everything it renders, resolves the folder location through `useSliceContext()`, and the command and query
 * hooks it uses register in the folder scope. Slice locations of those commands and queries refine the folder location
 * when they lie below it, for instance a slice below a feature folder.
 *
 * The returned component keeps the name of the component for React DevTools and Fast Refresh, forwards refs for
 * `forwardRef`, class and lazy components, and stays a `memo` component when given one. A value that is not a
 * component type is returned as it is.
 * @param component The component to wrap.
 * @param location The slice location of the folder.
 * @param exportedAs Optional name to give the returned component when the component has no name starting with an uppercase
 * letter, typically the name it is exported as, so DevTools and Fast Refresh recognize it as a component.
 * @returns The component rendering within the folder slice scope.
 */
export function withSliceLocation<TComponent>(component: TComponent, location: SliceLocation, exportedAs?: string): TComponent {
    if (!isComponentType(component)) {
        return component;
    }

    const ownName = nameOf(component);
    const name = ownName && /^[A-Z]/.test(ownName) ? ownName : (exportedAs ?? ownName);
    const type = typeOf(component);

    if (type === memoType) {
        const memoized = component as ReactTypeObject;
        const inner = withSliceLocation(memoized.type, location, exportedAs) as ComponentType<AnyProps>;
        return named(memo(inner, memoized.compare), memoized.displayName) as TComponent;
    }

    const target = component as ComponentType<AnyProps>;

    if (type === forwardRefType || type === lazyType || isClassComponent(component)) {
        const scoped = forwardRef<unknown, AnyProps>((props, ref) =>
            createElement(FolderSlice, { location }, createElement(target, { ...props, ref: ref as Ref<unknown> })));
        return named(scoped, name) as TComponent;
    }

    const scoped = (props: AnyProps) => createElement(FolderSlice, { location }, createElement(target, props));
    return named(scoped, name) as TComponent;
}
