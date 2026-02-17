// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyAccessor } from '@cratis/fundamentals';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import React, { useState, useEffect, ComponentType } from 'react';
import { useCommandFormContext } from './CommandForm';

/**
 * Props that will be injected by CommandFormFields into your wrapped component
 */
export interface InjectedCommandFormFieldProps {
    currentValue?: unknown;
    onValueChange?: (value: unknown) => void;
    propertyDescriptor?: PropertyDescriptor;
    fieldName?: string;
}

/**
 * Props that your field component should accept (excluding the injected ones)
 */
export interface BaseCommandFormFieldProps<TCommand> {
    icon?: React.ReactElement;
    value: PropertyAccessor<TCommand>;
    required?: boolean;
    title?: string;
    description?: string;
}

/**
 * Configuration for the field wrapper
 */
export interface CommandFormFieldConfig<TValue = unknown> {
    /** Default value when currentValue is undefined */
    defaultValue: TValue;
    /** Value extractor from the change event */
    extractValue?: (event: unknown) => TValue;
}

/**
 * Props that your wrapped component will receive
 */
export interface WrappedFieldProps<TValue = unknown> {
    value: TValue;
    onChange: (valueOrEvent: TValue | unknown) => void;
    invalid: boolean;
    required: boolean;
    errors: string[];
}

/**
 * Wraps a field component to work with CommandForm, handling all integration automatically.
 * 
 * @example
 * ```typescript
 * interface MyInputProps extends WrappedFieldProps<string> {
 *     placeholder?: string;
 * }
 * 
 * export const MyInputField = asCommandFormField<MyInputProps>(
 *     (props) => (
 *         <div>
 *             <input
 *                 value={props.value}
 *                 onChange={props.onChange}
 *                 placeholder={props.placeholder}
 *                 className={props.invalid ? 'invalid' : ''}
 *             />
 *             {props.errors.length > 0 && (
 *                 <div className="error-messages">
 *                     {props.errors.map((error, idx) => (
 *                         <small key={idx} className="p-error">{error}</small>
 *                     ))}
 *                 </div>
 *             )}
 *         </div>
 *     ),
 *     {
 *         defaultValue: '',
 *         extractValue: (e) => e.target.value
 *     }
 * );
 * ```
 */
export function asCommandFormField<TComponentProps extends WrappedFieldProps<unknown>>(
    component: ComponentType<TComponentProps> | ((props: TComponentProps) => React.ReactElement),
    config: CommandFormFieldConfig<TComponentProps['value']>
) {
    const { defaultValue, extractValue } = config;
    const Component = typeof component === 'function' && !component.prototype?.render 
        ? component 
        : component as ComponentType<TComponentProps>;

    const WrappedField = <TCommand,>(
        props: Omit<TComponentProps, keyof WrappedFieldProps> & 
               BaseCommandFormFieldProps<TCommand> & 
               InjectedCommandFormFieldProps
    ) => {
        const { 
            currentValue, 
            onValueChange, 
            fieldName, 
            required = true,
            ...componentProps 
        } = props;

        const [localValue, setLocalValue] = useState(currentValue ?? defaultValue);
        const { getFieldError, customFieldErrors } = useCommandFormContext();

        const serverError = fieldName ? getFieldError(fieldName) : undefined;
        const customError = fieldName ? customFieldErrors[fieldName] : undefined;
        
        const errors: string[] = [];
        if (serverError) errors.push(serverError);
        if (customError) errors.push(customError);

        const isInvalid = errors.length > 0;

        useEffect(() => {
            setLocalValue(currentValue ?? defaultValue);
        }, [currentValue]);

        const handleChange = (valueOrEvent: unknown) => {
            const newValue = extractValue ? extractValue(valueOrEvent) : valueOrEvent;
            setLocalValue(newValue);
            onValueChange?.(newValue);
        };

        const wrappedProps = {
            ...componentProps,
            value: localValue,
            onChange: handleChange,
            invalid: isInvalid,
            required,
            errors
        } as TComponentProps;

        return <Component {...wrappedProps} />;
    };

    WrappedField.displayName = 'CommandFormField';

    return WrappedField;
}
