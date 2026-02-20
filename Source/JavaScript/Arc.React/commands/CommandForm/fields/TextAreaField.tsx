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
            onBlur={props.onBlur}
            required={props.required}
            placeholder={props.placeholder}
            rows={props.rows ?? 5}
            cols={props.cols}
            className={props.className || ''}
            style={{ 
                width: '100%', 
                padding: '0.75rem',
                fontSize: '1rem',
                border: props.invalid ? '1px solid #ef4444' : '1px solid var(--color-border, #d1d5db)',
                borderRadius: '0.375rem',
                boxSizing: 'border-box',
                ...(props.hasLeftAddon ? { borderTopLeftRadius: 0, borderBottomLeftRadius: 0, alignSelf: 'stretch' } : {}),
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
