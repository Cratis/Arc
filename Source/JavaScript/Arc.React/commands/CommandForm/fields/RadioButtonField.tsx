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

export interface RadioButtonFieldProps<TAccessor extends RadioValueAccessor>
    extends Omit<BaseCommandFormFieldProps<CommandType<TAccessor>>, 'value'>,
    TypedInjectedRadioFieldProps<CommandValue<TAccessor>> {
    value: TAccessor;
    setValue: CommandValue<TAccessor>;
    label?: React.ReactNode;
    className?: string;
    style?: React.CSSProperties;
    disabled?: boolean;
    hasLeftAddon?: boolean;
}

export function RadioButtonField<TAccessor extends RadioValueAccessor>(props: RadioButtonFieldProps<TAccessor>): React.ReactElement {
    const handleChange = () => {
        props.onValueChange?.(props.setValue);
    };

    return (
        <label
            className={props.className || ''}
            style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
                padding: '0.75rem',
                border: props.invalid ? '1px solid #ef4444' : '1px solid var(--color-border, #d1d5db)',
                borderRadius: '0.375rem',
                width: '100%',
                boxSizing: 'border-box',
                cursor: props.disabled ? 'not-allowed' : 'pointer',
                opacity: props.disabled ? 0.6 : 1,
                ...(props.hasLeftAddon ? { borderTopLeftRadius: 0, borderBottomLeftRadius: 0, alignSelf: 'stretch' } : {}),
                ...props.style
            }}
        >
            <input
                type="radio"
                name={props.fieldName}
                checked={Object.is(props.currentValue, props.setValue)}
                onChange={handleChange}
                onBlur={props.onBlur}
                required={props.required}
                disabled={props.disabled}
                aria-invalid={props.invalid || undefined}
            />
            {props.label}
        </label>
    );
}

RadioButtonField.displayName = 'CommandFormField';
