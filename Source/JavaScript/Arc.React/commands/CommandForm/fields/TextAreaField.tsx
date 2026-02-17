// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface TextAreaFieldComponentProps extends WrappedFieldProps<string> {
    placeholder?: string;
    rows?: number;
    cols?: number;
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
            className={`w-full p-3 rounded-md text-base ${props.invalid ? 'border border-red-500' : 'border border-gray-300'}`}
        />
    ),
    {
        defaultValue: '',
        extractValue: (e: React.ChangeEvent<HTMLTextAreaElement>) => e.target.value
    }
);
