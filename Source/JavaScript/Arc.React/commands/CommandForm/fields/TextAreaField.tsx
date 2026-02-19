// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface TextAreaFieldComponentProps extends WrappedFieldProps<string> {
    placeholder?: string;
    rows?: number;
    cols?: number;
    className?: string;
    style?: React.CSSProperties;
    hasLeftAddon?: boolean;
}

export const TextAreaField = asCommandFormField<TextAreaFieldComponentProps>(
    (props) => (
        <textarea
            value={props.value}
            onChange={props.onChange}
            required={props.required}
            placeholder={props.placeholder}
            rows={props.rows ?? 5}
            cols={props.cols}
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
        defaultValue: '',
        extractValue: (e: unknown) => {
            if (e && typeof e === 'object' && 'target' in e) {
                const event = e as React.ChangeEvent<HTMLTextAreaElement>;
                return event.target.value;
            }
            return String(e || '');
        }
    }
);
