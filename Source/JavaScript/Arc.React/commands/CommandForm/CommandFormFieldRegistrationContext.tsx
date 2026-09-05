// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { deepEqual } from '@cratis/arc';
import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import type { CommandFormFieldRegistrationDescriptor } from './CommandFormFieldRegistrationDescriptor';

export const CommandFormFieldRegistrationContext = React.createContext<
    | {
          register: (
              id: symbol,
              descriptor: CommandFormFieldRegistrationDescriptor,
          ) => void;
          notifyChanged: (id: symbol) => void;
          unregister: (id: symbol) => void;
      }
    | undefined
>(undefined);

/**
 * Registers stable metadata for the field a CommandFormFieldWrapper actually binds.
 * @param fieldProps The rendered field's population props.
 * @param propertyName The command property resolved from the field accessor.
 */
export function useCommandFormFieldRegistration(
    fieldProps: CommandFormFieldProps,
    propertyName: string,
): void {
    const registration = React.useContext(CommandFormFieldRegistrationContext);
    const id = React.useRef(Symbol('CommandFormField'));
    const descriptorRef = React.useRef<CommandFormFieldRegistrationDescriptor>();
    if (descriptorRef.current === undefined) {
        descriptorRef.current = {
            propertyName,
            currentValue: fieldProps.currentValue,
            noInitialValue: fieldProps.noInitialValue === true,
            populationKey: fieldProps.populationKey,
            populationRevision: 0,
            valueAccessorRef: { current: undefined },
            initialValueRef: { current: undefined },
        };
    }
    const descriptor = descriptorRef.current;

    React.useLayoutEffect(() => {
        // The descriptor is shared with CommandForm after registration. Publish callbacks only from
        // committed renders: mutating these refs during render would let an abandoned concurrent
        // render replace the callbacks used by the still-mounted field.
        descriptor.valueAccessorRef.current = fieldProps.value as
            | ((instance: unknown) => unknown)
            | undefined;
        descriptor.initialValueRef.current = fieldProps.initialValue as
            | ((source: unknown) => unknown)
            | undefined;

        const propertyNameChanged = descriptor.propertyName !== propertyName;
        const currentValueChanged = !deepEqual(
            descriptor.currentValue,
            fieldProps.currentValue,
        );
        const noInitialValue = fieldProps.noInitialValue === true;
        const noInitialValueChanged = descriptor.noInitialValue !== noInitialValue;
        const populationKeyChanged = !deepEqual(
            descriptor.populationKey,
            fieldProps.populationKey,
        );

        if (
            !propertyNameChanged &&
            !currentValueChanged &&
            !noInitialValueChanged &&
            !populationKeyChanged
        ) {
            return;
        }

        descriptor.propertyName = propertyName;
        descriptor.currentValue = fieldProps.currentValue;
        descriptor.noInitialValue = noInitialValue;
        descriptor.populationKey = fieldProps.populationKey;
        if (propertyNameChanged || noInitialValueChanged || populationKeyChanged) {
            descriptor.populationRevision++;
        }
        registration?.notifyChanged(id.current);
    });

    React.useLayoutEffect(() => {
        registration?.register(id.current, descriptor);
        return () => registration?.unregister(id.current);
    }, [descriptor, registration]);
}
