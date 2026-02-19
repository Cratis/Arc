// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface InputTextComponentProps extends WrappedFieldProps<string> {
    type?: 'text' | 'email' | 'password' | 'color' | 'date' | 'datetime-local' | 'time' | 'url' | 'tel' | 'search';
    placeholder?: string;
    className?: string;
    style?: React.CSSProperties;
    hasLeftAddon?: boolean;
}

export const InputTextField = asCommandFormField<InputTextComponentProps>(
    (props) => (
        <input
            type={props.type || 'text'}
            value={props.value}
            onChange={props.onChange}
            required={props.required}
            placeholder={props.placeholder}
            className={`w-full p-3 text-base rounded-md ${props.invalid ? 'border border-red-500' : 'border border-gray-300'} ${props.className || ''}`}
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
        defaultValue: '',
        extractValue: (e: unknown) => {
            if (e && typeof e === 'object' && 'target' in e) {
                const event = e as React.ChangeEvent<HTMLInputElement>;
                return event.target.value;
            }
            return String(e || '');
        }
    }
);
