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
          unregister: (id: symbol) => void;
      }
    | undefined
>(undefined);

const getCallbackSourceSignature = (callback: unknown): string =>
    typeof callback === 'function'
        ? Function.prototype.toString.call(callback).replace(/\s+/g, ' ').trim()
        : '';

const registrationDescriptorsMatch = (
    first: CommandFormFieldRegistrationDescriptor,
    second: CommandFormFieldRegistrationDescriptor,
): boolean =>
    first.propertyName === second.propertyName &&
    deepEqual(first.currentValue, second.currentValue) &&
    first.noInitialValue === second.noInitialValue &&
    first.valueAccessorSourceSignature === second.valueAccessorSourceSignature &&
    first.initialValueSourceSignature === second.initialValueSourceSignature;

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
    const descriptor: CommandFormFieldRegistrationDescriptor = {
        propertyName,
        currentValue: fieldProps.currentValue,
        noInitialValue: fieldProps.noInitialValue === true,
        valueAccessorSourceSignature: getCallbackSourceSignature(fieldProps.value),
        initialValueSourceSignature: getCallbackSourceSignature(fieldProps.initialValue),
        initialValue: fieldProps.initialValue as
            | ((source: unknown) => unknown)
            | undefined,
    };

    if (
        descriptorRef.current === undefined ||
        !registrationDescriptorsMatch(descriptorRef.current, descriptor)
    ) {
        descriptorRef.current = descriptor;
    }
    const stableDescriptor = descriptorRef.current;

    React.useLayoutEffect(() => {
        registration?.register(id.current, stableDescriptor);
    }, [registration, stableDescriptor]);

    React.useLayoutEffect(
        () => () => registration?.unregister(id.current),
        [registration],
    );
}
