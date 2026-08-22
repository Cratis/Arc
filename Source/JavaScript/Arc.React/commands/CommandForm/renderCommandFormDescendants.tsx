// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import {
    canBindCommandFormFieldAtRuntime,
    isCommandFormColumn,
    isCommandFormField,
} from './commandFormMarkers';

/**
 * Recursively preserves framework-owned and intrinsic child trees while replacing marked fields.
 * Runtime-binding fields cross custom components unchanged so those components can filter or clone
 * them. Legacy marker-only fields use the compatibility fallback: their custom ancestor is cloned and
 * the legacy field is statically wrapped, because an old marker provides no runtime cooperation seam.
 * @param children The descendants to render.
 * @param renderField Renders a discovered command form field with its binding wrapper.
 * @returns The original descendant shape with marked fields replaced.
 */
const containsLegacyMarkerOnlyField = (children: React.ReactNode): boolean => {
    let containsLegacyField = false;
    React.Children.forEach(children, (child) => {
        if (containsLegacyField || !React.isValidElement(child)) return;
        const component = child.type as React.ComponentType<unknown>;
        if (isCommandFormField(component)) {
            containsLegacyField = !canBindCommandFormFieldAtRuntime(component);
            return;
        }
        containsLegacyField = containsLegacyMarkerOnlyField(
            (child.props as { children?: React.ReactNode }).children,
        );
    });
    return containsLegacyField;
};

export function renderCommandFormDescendants(
    children: React.ReactNode,
    renderField: (field: React.ReactElement<CommandFormFieldProps>) => React.ReactElement,
    crossingCustomBoundary = false,
): React.ReactNode {
    return React.Children.map(children, (child) => {
        if (!React.isValidElement(child)) {
            return child;
        }

        const component = child.type as React.ComponentType<unknown>;
        if (isCommandFormField(component)) {
            if (crossingCustomBoundary && canBindCommandFormFieldAtRuntime(component)) {
                return child;
            }
            return renderField(child as React.ReactElement<CommandFormFieldProps>);
        }

        if (isCommandFormColumn(component)) {
            return child;
        }

        const isFrameworkOwnedTree = child.type === React.Fragment;
        const isIntrinsicElement = typeof child.type === 'string';
        const childProps = child.props as { children?: React.ReactNode };
        if (!isFrameworkOwnedTree && !isIntrinsicElement) {
            if (!containsLegacyMarkerOnlyField(childProps.children)) {
                return child;
            }
            return React.cloneElement(
                child,
                undefined,
                renderCommandFormDescendants(childProps.children, renderField, true),
            );
        }

        const renderedChildren = renderCommandFormDescendants(
            childProps.children,
            renderField,
            crossingCustomBoundary,
        );
        return React.cloneElement(child, undefined, renderedChildren);
    });
}
