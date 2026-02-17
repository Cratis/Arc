// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';

interface RangeComponentProps extends WrappedFieldProps<number> {
    min?: number;
    max?: number;
    step?: number;
}

export const RangeField = asCommandFormField<RangeComponentProps>(
    (props) => {
        const min = props.min ?? 0;
        const max = props.max ?? 100;
        const step = props.step ?? 1;

        return (
            <div className="w-full flex items-center gap-4 p-3 border border-gray-300 rounded-md">
                <input
                    type="range"
                    value={props.value}
                    onChange={props.onChange}
                    min={min}
                    max={max}
                    step={step}
                    required={props.required}
                    className="flex-1"
                />
                <span className="min-w-[3rem] text-right font-semibold">
                    {props.value}
                </span>
            </div>
        );
    },
    {
        defaultValue: 0,
        extractValue: (e: React.ChangeEvent<HTMLInputElement>) => parseFloat(e.target.value)
    }
);
