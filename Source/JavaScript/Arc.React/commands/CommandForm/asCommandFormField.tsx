// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyDescriptor } from '@cratis/arc/reflection';
import React, { ComponentType } from 'react';
import { useCommandFormContext } from './CommandForm';

/**
 * Props that will be injected by CommandFormFields into your wrapped component
 */
export interface InjectedCommandFormFieldProps {
    currentValue?: unknown;
    onValueChange?: (value: unknown) => void;
    onBlur?: () => void;
    propertyDescriptor?: PropertyDescriptor;
    fieldName?: string;
}

/**
 * Props that your field component should accept (excluding the injected ones).
 *
 * The value accessor uses a method declaration so that TypeScript checks it
 * bivariantly.  This lets callers pass `(c: MyCommand) => c.name` even when
 * the generic defaults to `unknown`, while still providing full type safety
 * when an explicit type argument is supplied:
 *
 *   <MyField<MyCommand> value={c => c.name} />
 */
export interface BaseCommandFormFieldProps<TCommand = unknown> {
    icon?: React.ReactElement;
    value(instance: TCommand): unknown;
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
    onBlur?: () => void;
    invalid: boolean;
    required: boolean;
    errors: string[];
}

/**
 * The public props for a field component produced by asCommandFormField.
 * TCommand defaults to unknown; callers should specify the command type
 * explicitly for full type safety on the value accessor.
 */
export type CommandFormFieldComponentProps<TComponentProps extends WrappedFieldProps, TCommand = unknown> =
    Omit<TComponentProps, keyof WrappedFieldProps> & BaseCommandFormFieldProps<TCommand> & InjectedCommandFormFieldProps;

/**
 * Wraps a field component to work with CommandForm, handling all integration automatically.
 * The returned component is generic in TCommand, providing type safety for the value accessor
 * while defaulting to any so explicit type arguments aren't required.
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
 *
 * // Usage with explicit type (full type safety):
 * <MyInputField<SimpleCommand> value={c => c.name} placeholder="Name" />
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

    const WrappedField = <TCommand = unknown,>(
        props: CommandFormFieldComponentProps<TComponentProps, TCommand>
    ): React.ReactElement => {
        const {
            currentValue,
            onValueChange,
            onBlur,
            fieldName,
            propertyDescriptor,
            required,
            ...componentProps
        } = props;

        const { getFieldError, customFieldErrors } = useCommandFormContext();

        // Determine if field is required based on PropertyDescriptor or explicit prop
        const isRequired = required ?? (propertyDescriptor ? !propertyDescriptor.isOptional : true);

        const serverError = fieldName ? getFieldError(fieldName) : undefined;
        const customError = fieldName ? customFieldErrors[fieldName] : undefined;

        const errors: string[] = [];
        if (serverError) errors.push(serverError);
        if (customError) errors.push(customError);

        const isInvalid = errors.length > 0;

        const handleChange = (valueOrEvent: unknown) => {
            const newValue = extractValue ? extractValue(valueOrEvent) : valueOrEvent;
            onValueChange?.(newValue);
        };

        const displayValue = currentValue !== undefined ? currentValue : defaultValue;

        const wrappedProps = {
            ...componentProps,
            value: displayValue,
            onChange: handleChange,
            onBlur,
            invalid: isInvalid,
            required: isRequired,
            errors
        } as TComponentProps;

        return <Component {...wrappedProps} />;
    };

    WrappedField.displayName = 'CommandFormField';

    return WrappedField;
}
