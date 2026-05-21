// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import type { BaseCommandFormFieldProps, InjectedCommandFormFieldProps } from '../asCommandFormField';

type RadioValueAccessor = (instance: never) => unknown;
type CommandType<TAccessor extends RadioValueAccessor> = Parameters<TAccessor>[0];
type CommandValue<TAccessor extends RadioValueAccessor> = ReturnType<TAccessor>;

type TypedInjectedRadioFieldProps<TValue> = Omit<InjectedCommandFormFieldProps, 'currentValue' | 'onValueChange'> & {
    currentValue?: TValue;
    onValueChange?: (value: TValue) => void;
    invalid?: boolean;
};

export interface RadioGroupFieldOption<TValue> {
    value: TValue;
    label: React.ReactNode;
    disabled?: boolean;
}

export interface RadioGroupFieldProps<TAccessor extends RadioValueAccessor>
    extends Omit<BaseCommandFormFieldProps<CommandType<TAccessor>>, 'value'>,
    TypedInjectedRadioFieldProps<CommandValue<TAccessor>> {
    value: TAccessor;
    options: RadioGroupFieldOption<CommandValue<TAccessor>>[];
    direction?: 'horizontal' | 'vertical';
    className?: string;
    style?: React.CSSProperties;
    hasLeftAddon?: boolean;
}

export function RadioGroupField<TAccessor extends RadioValueAccessor>(props: RadioGroupFieldProps<TAccessor>): React.ReactElement {
    return (
        <div
            className={props.className || ''}
            style={{
                display: 'flex',
                flexDirection: props.direction === 'horizontal' ? 'row' : 'column',
                gap: '0.75rem',
                padding: '0.75rem',
                border: props.invalid ? '1px solid #ef4444' : '1px solid var(--color-border, #d1d5db)',
                borderRadius: '0.375rem',
                width: '100%',
                boxSizing: 'border-box',
                ...(props.hasLeftAddon ? { borderTopLeftRadius: 0, borderBottomLeftRadius: 0, alignSelf: 'stretch' } : {}),
                ...props.style
            }}
        >
            {props.options.map((option, index) => (
                <label
                    key={index}
                    style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '0.5rem',
                        cursor: option.disabled ? 'not-allowed' : 'pointer',
                        opacity: option.disabled ? 0.6 : 1
                    }}
                >
                    <input
                        type="radio"
                        name={props.fieldName}
                        checked={Object.is(props.currentValue, option.value)}
                        onChange={() => props.onValueChange?.(option.value)}
                        onBlur={props.onBlur}
                        required={props.required}
                        disabled={option.disabled}
                        aria-invalid={props.invalid || undefined}
                    />
                    {option.label}
                </label>
            ))}
        </div>
    );
}

RadioGroupField.displayName = 'CommandFormField';
