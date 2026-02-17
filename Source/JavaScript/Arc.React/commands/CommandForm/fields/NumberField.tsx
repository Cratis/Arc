// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface NumberFieldComponentProps extends WrappedFieldProps<number> {
    placeholder?: string;
    min?: number;
    max?: number;
    step?: number;
}

export const NumberField = asCommandFormField<NumberFieldComponentProps>(
    (props) => (
        <input
            type="number"
            value={props.value}
            onChange={props.onChange}
            required={props.required}
            placeholder={props.placeholder}
            min={props.min}
            max={props.max}
            step={props.step}
            className={`w-full p-3 rounded-md text-base ${props.invalid ? 'border border-red-500' : 'border border-gray-300'}`}
        />
    ),
    {
        defaultValue: 0,
        extractValue: (e: unknown) => {
            if (e && typeof e === 'object' && 'target' in e) {
                const event = e as React.ChangeEvent<HTMLInputElement>;
                return parseFloat(event.target.value) || 0;
            }
            return typeof e === 'number' ? e : 0;
        }
    }
);
