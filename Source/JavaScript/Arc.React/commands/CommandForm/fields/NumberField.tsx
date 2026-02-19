// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface NumberFieldComponentProps extends WrappedFieldProps<number> {
    placeholder?: string;
    min?: number;
    max?: number;
    step?: number;
    className?: string;
    style?: React.CSSProperties;
    hasLeftAddon?: boolean;
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
            className={`w-full p-3 rounded-md text-base ${props.invalid ? 'border border-red-500' : 'border border-gray-300'} ${props.className || ''}`}
            style={{ 
                width: '100%', 
                display: 'block',
                padding: '0.75rem',
                fontSize: '1rem',
                border: '1px solid var(--color-border)',
                borderRadius: '0.375rem',
                boxSizing: 'border-box',
                ...(props.hasLeftAddon ? { borderTopLeftRadius: 0, borderBottomLeftRadius: 0 } : {}),
                ...props.style 
            }}
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
