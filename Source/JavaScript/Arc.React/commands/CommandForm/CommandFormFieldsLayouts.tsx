// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { isCommandFormColumn } from './commandFormMarkers.js';
import { renderCommandFormDescendants } from './renderCommandFormDescendants.js';

type RenderField = Parameters<typeof renderCommandFormDescendants>[1];

export interface OrderedChild {
    type: 'field' | 'other';
    content: React.ReactNode;
    index: number;
    key?: React.Key;
}

const isColumnElement = (
    node: React.ReactNode,
): node is React.ReactElement<{ children?: React.ReactNode }> =>
    React.isValidElement(node) &&
    isCommandFormColumn(node.type as React.ComponentType<unknown>);

const getNodeKey = (node: React.ReactNode, fallback: string): string =>
    React.isValidElement(node) && node.key !== null ? String(node.key) : fallback;

const getItemKey = (item: OrderedChild): string =>
    String(item.key ?? getNodeKey(item.content, `${item.type}-${item.index}`));

/** Whether any of the ordered children is a direct column element. */
export const hasDirectColumns = (orderedChildren: OrderedChild[]): boolean =>
    orderedChildren.some((item) => isColumnElement(item.content));

/** Renders ordered children that include direct columns side by side, other content full width. */
export const OrderedColumnsLayout = ({
    orderedChildren,
    renderField,
}: {
    orderedChildren: OrderedChild[];
    renderField: RenderField;
}) => {
    const containsOnlyColumns = orderedChildren.every((item) => isColumnElement(item.content));
    const className = containsOnlyColumns
        ? 'card flex flex-column md:flex-row gap-3'
        : 'card flex flex-column md:flex-row gap-3 flex-wrap';

    return (
        <div className={className}>
            {orderedChildren.map((item) => {
                const key = getItemKey(item);
                if (isColumnElement(item.content)) {
                    return (
                        <div key={key} className='flex flex-column gap-3 flex-1'>
                            {renderCommandFormDescendants(item.content.props.children, renderField)}
                        </div>
                    );
                }
                return (
                    <div key={key} className='w-full' style={{ flexBasis: '100%', width: '100%' }}>
                        {renderCommandFormDescendants(item.content, renderField)}
                    </div>
                );
            })}
        </div>
    );
};

/** Renders ordered children stacked in a single column. */
export const OrderedStackLayout = ({
    orderedChildren,
    renderField,
}: {
    orderedChildren: OrderedChild[];
    renderField: RenderField;
}) => (
    <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
        {orderedChildren.map((item) => (
            <React.Fragment key={getItemKey(item)}>
                {renderCommandFormDescendants(item.content, renderField)}
            </React.Fragment>
        ))}
    </div>
);
