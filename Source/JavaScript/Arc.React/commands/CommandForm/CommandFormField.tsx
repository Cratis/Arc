// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { PropertyAccessor } from '@cratis/fundamentals';

export interface CommandFormFieldProps<TCommand = unknown> {
    icon?: React.ReactElement;
    /** Accessor function that selects a property on the command, e.g. c => c.name */
    value?: PropertyAccessor<TCommand>;
    /** Current value for the property (injected by CommandFormFields) */
    currentValue?: unknown;
    /** Called when the field value changes (injected by CommandFormFields) */
    onValueChange?: (value: unknown) => void;
    onChange?: (value: unknown) => void;
    required?: boolean;
    title?: string;
    description?: string;
    propertyDescriptor?: unknown;
    fieldName?: string;
}

export const CommandFormField = <TCommand,>(_props?: CommandFormFieldProps<TCommand>) => {
    void _props;
    return <></>;
};

CommandFormField.displayName = 'CommandFormField';
