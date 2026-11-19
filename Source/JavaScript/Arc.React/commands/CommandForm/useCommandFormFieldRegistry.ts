// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useCallback, useMemo, useState } from 'react';
import { deepEqual } from '@cratis/arc';
import type { CommandFormFieldRegistrationDescriptor } from './CommandFormFieldRegistrationDescriptor.js';

type TransformedPopulation = {
    source: object;
    populationRevision: number;
    value: unknown;
};

const getInitialValuesFromFields = <TCommand,>(
    registrations: CommandFormFieldRegistrationDescriptor[],
): Partial<TCommand> => {
    const values: Record<string, unknown> = {};
    registrations.forEach((registration) => {
        if (registration.currentValue !== undefined && registration.propertyName) {
            values[registration.propertyName] = registration.currentValue;
        }
    });
    return values as Partial<TCommand>;
};

/**
 * Resolves the value a field contributes from the population source: the transformed value of its
 * initialValue callback (cached per field), or the property of the same name on the source.
 */
const resolvePopulatedValue = (
    id: symbol,
    registration: CommandFormFieldRegistrationDescriptor,
    populatedSource: object,
    cache: Map<symbol, TransformedPopulation>,
): { found: boolean; value?: unknown } => {
    const initialValue = registration.initialValueRef.current;
    if (!initialValue) {
        const propertyName = registration.propertyName as string;
        return Object.hasOwn(populatedSource, propertyName)
            ? { found: true, value: (populatedSource as Record<string, unknown>)[propertyName] }
            : { found: false };
    }

    const cached = cache.get(id);
    if (
        cached !== undefined &&
        deepEqual(cached.source, populatedSource) &&
        cached.populationRevision === registration.populationRevision
    ) {
        return { found: true, value: cached.value };
    }

    const value = initialValue(populatedSource);
    cache.set(id, {
        source: populatedSource,
        populationRevision: registration.populationRevision,
        value,
    });
    return { found: true, value };
};

const collectPopulatedValues = (
    registrations: Map<symbol, CommandFormFieldRegistrationDescriptor>,
    populatedSource: object,
    cache: Map<symbol, TransformedPopulation>,
): Record<string, unknown> => {
    const values: Record<string, unknown> = {};
    registrations.forEach((registration, id) => {
        if (registration.noInitialValue || !registration.propertyName) {
            return;
        }

        const resolved = resolvePopulatedValue(id, registration, populatedSource, cache);
        if (resolved.found) {
            values[registration.propertyName] = resolved.value;
        }
    });
    return values;
};

/**
 * Tracks the fields registered with a command form and derives the values they contribute:
 * the initial values from field currentValue props, and the values populated from a query source.
 * @param {object | undefined} populatedSource The resolved result of the populate query, if any.
 * @returns The field registration context value and the derived values.
 */
export function useCommandFormFieldRegistry<TCommand>(populatedSource: object | undefined) {
    const registeredFieldsById = React.useRef(
        new Map<symbol, CommandFormFieldRegistrationDescriptor>(),
    );
    const transformedPopulationById = React.useRef(new Map<symbol, TransformedPopulation>());
    const [registeredFieldsVersion, setRegisteredFieldsVersion] = useState(0);
    const registerField = useCallback(
        (id: symbol, descriptor: CommandFormFieldRegistrationDescriptor) => {
            if (registeredFieldsById.current.get(id) === descriptor) {
                return;
            }
            registeredFieldsById.current.set(id, descriptor);
            setRegisteredFieldsVersion((version) => version + 1);
        },
        [],
    );
    const notifyFieldChanged = useCallback((id: symbol) => {
        if (registeredFieldsById.current.has(id)) {
            setRegisteredFieldsVersion((version) => version + 1);
        }
    }, []);
    const unregisterField = useCallback((id: symbol) => {
        transformedPopulationById.current.delete(id);
        if (registeredFieldsById.current.delete(id)) {
            setRegisteredFieldsVersion((version) => version + 1);
        }
    }, []);
    const fieldRegistration = useMemo(
        () => ({
            register: registerField,
            notifyChanged: notifyFieldChanged,
            unregister: unregisterField,
        }),
        [registerField, notifyFieldChanged, unregisterField],
    );
    const registeredFields = useMemo(
        () => Array.from(registeredFieldsById.current.values()),
        [registeredFieldsVersion],
    );
    const initialValuesFromFields = useMemo(
        () => getInitialValuesFromFields<TCommand>(registeredFields),
        [registeredFields],
    );

    // Match registered metadata onto the resolved source after commit. Field layout effects publish
    // their latest callbacks first, so a source and callback that change in the same commit are
    // evaluated together. This also keeps abandoned concurrent renders from affecting population.
    // Transformed values are cached per field, so registering another field does not re-evaluate
    // existing initialValue callbacks.
    const [populatedValues, setPopulatedValues] = useState<Partial<TCommand>>({});
    React.useLayoutEffect(() => {
        if (populatedSource === undefined) {
            setPopulatedValues((previous) =>
                Object.keys(previous).length === 0 ? previous : {},
            );
            return;
        }

        // SAFETY: Registration property names come from accessors over TCommand fields.
        const nextValues = collectPopulatedValues(
            registeredFieldsById.current,
            populatedSource,
            transformedPopulationById.current,
        ) as Partial<TCommand>;
        setPopulatedValues((previous) =>
            deepEqual(previous, nextValues) ? previous : nextValues,
        );
    }, [registeredFieldsVersion, populatedSource]);

    return { fieldRegistration, initialValuesFromFields, populatedValues };
}
