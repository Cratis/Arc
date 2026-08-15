// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { ColumnInfo } from './CommandFormFields';
import { getPropertyNameFromAccessor } from './getPropertyNameFromAccessor';

/**
 * Extracts a command's initial values from a population source (a query result, or a plain object),
 * matching each field onto the source by the property name its accessor resolves to - unless the
 * field opts out with `noInitialValue`, or overrides the derivation with `initialValue`.
 * @template TCommand Type of the command the fields belong to.
 * @param fieldsOrColumns The fields (or column groups of fields) a `CommandForm` renders.
 * @param source The population source, or `undefined` when none has resolved yet.
 * @returns The command's initial values derived from `source`.
 */
export function extractPopulatedValues<TCommand>(
    fieldsOrColumns: React.ReactElement[] | ColumnInfo[],
    source: object | undefined
): Partial<TCommand> {
    if (!source) {
        return {};
    }

    const fields = fieldsOrColumns.length > 0 && 'fields' in fieldsOrColumns[0]
        ? (fieldsOrColumns as ColumnInfo[]).flatMap(column => column.fields)
        : fieldsOrColumns as React.ReactElement[];

    const values: Record<string, unknown> = {};
    for (const field of fields) {
        const fieldProps = field.props as Record<string, unknown>;
        if (fieldProps.noInitialValue) {
            continue;
        }

        const propertyAccessor = fieldProps.value as ((instance: unknown) => unknown) | undefined;
        const propertyName = propertyAccessor ? getPropertyNameFromAccessor(propertyAccessor) : '';
        if (!propertyName) {
            continue;
        }

        const initialValue = fieldProps.initialValue as ((source: unknown) => unknown) | undefined;
        if (initialValue) {
            values[propertyName] = initialValue(source);
        } else if (Object.prototype.hasOwnProperty.call(source, propertyName)) {
            values[propertyName] = (source as Record<string, unknown>)[propertyName];
        }
    }

    return values as Partial<TCommand>;
}
