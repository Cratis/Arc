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
        required={props.required}
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
