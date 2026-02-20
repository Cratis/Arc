// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface SelectComponentProps extends WrappedFieldProps<string> {
    options: Array<{ [key: string]: unknown }>;
    optionIdField: string;
    optionLabelField: string;
    placeholder?: string;
    className?: string;
    style?: React.CSSProperties;
    hasLeftAddon?: boolean;
}

const SelectComponent = (props: SelectComponentProps) => (
    <select
        value={props.value || ''}
        onChange={props.onChange}
        onBlur={props.onBlur}
        required={props.required}
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
    >
        {props.placeholder && <option value="">{props.placeholder}</option>}
        {props.options.map((option, index) => (
            <option key={index} value={String(option[props.optionIdField])}>
                {String(option[props.optionLabelField])}
            </option>
        ))}
    </select>
);

export const SelectField = asCommandFormField<SelectComponentProps>(
    SelectComponent,
    {
        defaultValue: '',
        extractValue: (e: unknown) => {
            if (e && typeof e === 'object' && 'target' in e) {
                return (e.target as HTMLSelectElement).value;
            }
            return String(e);
        }
    }
);
