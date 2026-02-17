// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface InputTextComponentProps extends WrappedFieldProps<string> {
    type?: 'text' | 'email' | 'password' | 'color' | 'date' | 'datetime-local' | 'time' | 'url' | 'tel' | 'search';
    placeholder?: string;
}

export const InputTextField = asCommandFormField<InputTextComponentProps>(
    (props) => (
        <input
            type={props.type || 'text'}
            value={props.value}
            onChange={props.onChange}
            required={props.required}
            placeholder={props.placeholder}
            className={`w-full p-3 rounded-md text-base ${props.invalid ? 'border border-red-500' : 'border border-gray-300'}`}
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
