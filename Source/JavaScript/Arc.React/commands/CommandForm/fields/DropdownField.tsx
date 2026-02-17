// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface SelectComponentProps extends WrappedFieldProps<string> {
    options: Array<{ [key: string]: unknown }>;
    optionIdField: string;
    optionLabelField: string;
    placeholder?: string;
}

const SelectComponent = (props: SelectComponentProps) => (
    <select
        value={props.value || ''}
        onChange={props.onChange}
        required={props.required}
        className={`w-full p-3 rounded-md text-base ${props.invalid ? 'border border-red-500' : 'border border-gray-300'}`}
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
        extractValue: (e: React.ChangeEvent<HTMLSelectElement>) => e.target.value
    }
);
