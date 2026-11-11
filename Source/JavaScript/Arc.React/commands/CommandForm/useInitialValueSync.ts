// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { deepEqual } from '@cratis/arc';

const getChangedValues = (
    current: Record<string, unknown>,
    previous: Record<string, unknown>,
): Record<string, unknown> =>
    Object.fromEntries(
        Object.entries(current).filter(
            ([propertyName, value]) =>
                !Object.hasOwn(previous, propertyName) ||
                !deepEqual(previous[propertyName], value),
        ),
    );

const pickOwnValues = (
    values: Record<string, unknown>,
    propertyNames: Iterable<string>,
): Record<string, unknown> => {
    const picked: Record<string, unknown> = {};
    for (const propertyName of propertyNames) {
        if (Object.hasOwn(values, propertyName)) {
            picked[propertyName] = values[propertyName];
        }
    }
    return picked;
};

/** The sources of the form's values, the merged result of them, and how to apply a change to the command. */
export interface InitialValueSyncOptions<TCommand> {
    mergedInitialValues: Partial<TCommand>;
    populatedValues: Partial<TCommand>;
    initialValuesFromFields: Partial<TCommand>;
    valuesFromCurrentValues: Partial<TCommand>;
    validateOnInit?: boolean;
    setCommandValues: (values: TCommand) => void;
    setCommandInitialValues: (values: Partial<TCommand>) => void;
    validateSilently: (showErrors: boolean) => Promise<void>;
}

/**
 * Tracks values becoming available, not merely source arrival. Runtime fields register after the
 * first render and may mount after a query result is already cached. Applying only changed keys
 * preserves edits in unrelated fields while each seed/populated value becomes a true baseline.
 * @param {InitialValueSyncOptions} options The value sources and the functions applying them.
 */
export function useInitialValueSync<TCommand>(options: InitialValueSyncOptions<TCommand>) {
    const {
        mergedInitialValues,
        populatedValues,
        initialValuesFromFields,
        valuesFromCurrentValues,
        validateOnInit,
        setCommandValues,
        setCommandInitialValues,
        validateSilently,
    } = options;
    const initializedRef = React.useRef(false);
    const previousFieldInitialValuesRef = React.useRef<Partial<TCommand>>({});
    const previousPopulatedValuesRef = React.useRef<Partial<TCommand>>({});
    const previousCurrentValuesRef = React.useRef<Partial<TCommand>>(
        valuesFromCurrentValues,
    );
    React.useEffect(() => {
        const changedFieldInitialValues = getChangedValues(
            initialValuesFromFields as Record<string, unknown>,
            previousFieldInitialValuesRef.current as Record<string, unknown>,
        );
        const changedPopulatedValues = getChangedValues(
            populatedValues as Record<string, unknown>,
            previousPopulatedValuesRef.current as Record<string, unknown>,
        );
        const changedCurrentValues = getChangedValues(
            valuesFromCurrentValues as Record<string, unknown>,
            previousCurrentValuesRef.current as Record<string, unknown>,
        );
        previousFieldInitialValuesRef.current = initialValuesFromFields;
        previousPopulatedValuesRef.current = populatedValues;
        previousCurrentValuesRef.current = valuesFromCurrentValues;

        const baselineProperties = new Set([
            ...Object.keys(changedFieldInitialValues),
            ...Object.keys(changedPopulatedValues),
        ]);
        const mergedValues = mergedInitialValues as Record<string, unknown>;
        const reactiveValues = pickOwnValues(
            mergedValues,
            Object.keys(changedCurrentValues).filter(
                (propertyName) => !baselineProperties.has(propertyName),
            ),
        );
        const baselineValues = pickOwnValues(mergedValues, baselineProperties);

        if (Object.keys(reactiveValues).length > 0) {
            setCommandValues(reactiveValues as TCommand);
        }
        if (Object.keys(baselineValues).length > 0) {
            setCommandInitialValues(baselineValues as Partial<TCommand>);
        }

        const fieldInitialValuesChanged = baselineProperties.size > 0;
        const currentValuesChanged = Object.keys(changedCurrentValues).length > 0;

        // Always run silent client validation on init and after currentValues/populated source
        // changes to determine if the form is valid. This ensures isValid reflects the real validity
        // state from the first render, even when no error messages are shown yet. A populate query
        // only re-triggers validation when its resolved data actually changed, not on every
        // unrelated re-render. Error messages are only rendered (via commandResult) when
        // validateOnInit is true.
        if (!initializedRef.current) {
            initializedRef.current = true;
            void validateSilently(validateOnInit ?? false);
        } else if (currentValuesChanged || fieldInitialValuesChanged) {
            void validateSilently(validateOnInit ?? false);
        }
    }, [
        mergedInitialValues,
        populatedValues,
        initialValuesFromFields,
        valuesFromCurrentValues,
        validateOnInit,
        setCommandValues,
        setCommandInitialValues,
        validateSilently,
    ]);
}
