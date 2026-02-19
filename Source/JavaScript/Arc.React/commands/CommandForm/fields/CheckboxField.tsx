// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface CheckboxFieldComponentProps extends WrappedFieldProps<boolean> {
    label?: string;
    className?: string;
    style?: React.CSSProperties;
    hasLeftAddon?: boolean;
}

export const CheckboxField = asCommandFormField<CheckboxFieldComponentProps>(
    (props) => (
        <div 
            className={props.className || ''}
            style={{
                display: 'flex',
                alignItems: 'center',
                padding: '0.75rem',
                border: props.invalid ? '1px solid #ef4444' : '1px solid var(--color-border, #d1d5db)',
                borderRadius: '0.375rem',
                width: '100%',
                boxSizing: 'border-box',
                ...(props.hasLeftAddon ? { borderTopLeftRadius: 0, borderBottomLeftRadius: 0, alignSelf: 'stretch' } : {}),
                ...props.style
            }}
        >
            <input
                type="checkbox"
                checked={props.value}
                onChange={props.onChange}
                required={props.required}
                style={{ width: '1.25rem', height: '1.25rem' }}
            />
            {props.label && <label style={{ marginLeft: '0.5rem' }}>{props.label}</label>}
        </div>
    ),
    {
        defaultValue: false,
        extractValue: (e: unknown) => {
            if (typeof e === 'boolean') return e;
            if (e && typeof e === 'object' && 'target' in e) {
                const event = e as React.ChangeEvent<HTMLInputElement>;
                return event.target.checked;
            }
            return false;
        }
    }
);
