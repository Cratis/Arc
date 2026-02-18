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
            <div className="w-full flex items-center gap-4 p-3 border border-gray-300 rounded-md" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '0.75rem', border: '1px solid var(--color-border)', borderRadius: '0.375rem', backgroundColor: 'var(--color-background-secondary)' }}>
                <input
                    type="range"
                    value={props.value}
                    onChange={props.onChange}
                    min={min}
                    max={max}
                    step={step}
                    required={props.required}
                    className="flex-1"
                    style={{ flex: 1 }}
                />
                <span className="min-w-[3rem] text-right font-semibold" style={{ minWidth: '3rem', textAlign: 'right', fontWeight: 600, color: 'var(--color-text)' }}>
                    {props.value}
                </span>
            </div>
        );
    },
    {
        defaultValue: 0,
        extractValue: (e: unknown) => {
            if (e && typeof e === 'object' && 'target' in e) {
                return parseFloat((e.target as HTMLInputElement).value);
            }
            if (typeof e === 'number') return e;
            if (typeof e === 'string') return parseFloat(e);
            return 0;
        }
    }
);
