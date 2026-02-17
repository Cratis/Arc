// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { useCommandFormContext } from './CommandForm';

export interface ValidationMessageProps<TCommand> {
    value: (command: TCommand) => unknown;
}

export const ValidationMessage = <TCommand,>(props: ValidationMessageProps<TCommand>) => {
    const context = useCommandFormContext<TCommand>();
    const propertyAccessor = props.value;
    
    // Get the property name from the accessor function
    const getPropertyName = (accessor: (obj: TCommand) => unknown): string => {
        const fnStr = accessor.toString();
        const match = fnStr.match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);
        return match ? match[1] : '';
    };
    
    const propertyName = getPropertyName(propertyAccessor);
    const errorMessage = propertyName ? context.getFieldError(propertyName) : undefined;
    
    if (!errorMessage) {
        return null;
    }
    
    return (
        <small className="p-error block mt-1">{errorMessage}</small>
    );
};

ValidationMessage.displayName = 'ValidationMessage';
