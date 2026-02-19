// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCommandFormContext, type FieldValidationInfo } from './CommandFormContext';
import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import type { ICommandResult } from '@cratis/arc/commands';

export interface ColumnInfo {
    fields: React.ReactElement<CommandFormFieldProps>[];
}

export interface CommandFormFieldsProps {
    fields?: React.ReactElement<CommandFormFieldProps>[];
    columns?: ColumnInfo[];
    orderedChildren?: Array<{ type: 'field' | 'other', content: React.ReactNode, index: number }>;
}

// Separate component for each field to prevent re-rendering all fields
const CommandFormFieldWrapper = ({ field }: { field: React.ReactElement<CommandFormFieldProps> }) => {
    const context = useCommandFormContext<unknown>();
    const fieldProps = field.props as CommandFormFieldProps;
    const propertyAccessor = fieldProps.value;

    // Get the property name from the accessor function
    const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : '';

    // Get the current value from the command instance
    // commandVersion ensures this component re-renders when values change
    const currentValue = React.useMemo(() => {
        return propertyName ? (context.commandInstance as Record<string, unknown>)?.[propertyName] : undefined;
    }, [context.commandInstance, propertyName, context.commandVersion]);

    // Get the error message for this field, if any
    const errorMessage = propertyName ? context.getFieldError(propertyName) : undefined;

    // Get the property descriptor for this field from the command instance
    const propertyDescriptor = propertyName && (context.commandInstance as Record<string, unknown>)?.propertyDescriptors
        ? ((context.commandInstance as Record<string, unknown>).propertyDescriptors as Array<Record<string, unknown>>).find((pd: Record<string, unknown>) => pd.name === propertyName)
        : undefined;

    // Clone the field element with the current value and onChange handler
    const clonedField = React.cloneElement(field as React.ReactElement, {
        ...fieldProps,
        currentValue,
        propertyDescriptor,
        fieldName: propertyName,
        onValueChange: async (value: unknown) => {
            if (propertyName) {
                const oldValue = currentValue;

                // Update the command value
                context.setCommandValues({ [propertyName]: value } as Record<string, unknown>);

                // Call validate() on the command instance and store the result
                let validationResult: ICommandResult<unknown> | undefined = undefined;
                if (context.commandInstance && typeof (context.commandInstance as Record<string, unknown>).validate === 'function') {
                    validationResult = await ((context.commandInstance as Record<string, unknown>).validate as () => Promise<ICommandResult<unknown>>)();
                    if (validationResult) {
                        context.setCommandResult(validationResult);
                    }
                }

                // Call custom field validator if provided
                if (context.onFieldValidate) {
                    const validationError = context.onFieldValidate(context.commandInstance as Record<string, unknown>, propertyName, oldValue, value);
                    context.setCustomFieldError(propertyName, validationError);
                }

                // Call field change callback if provided
                if (context.onFieldChange) {
                    // Get field-specific validation info from the fresh validation result
                    const fieldErrors = validationResult?.validationResults?.filter(
                        vr => vr.members.includes(propertyName)
                    ).map(vr => vr.message) || [];
                    
                    const validationInfo: FieldValidationInfo = {
                        isValid: fieldErrors.length === 0,
                        errors: fieldErrors
                    };
                    
                    context.onFieldChange(context.commandInstance as Record<string, unknown>, propertyName, oldValue, value, validationInfo);
                }
            }
            fieldProps.onChange?.(value as unknown);
        },
        required: fieldProps.required ?? (propertyDescriptor ? !(propertyDescriptor as { isOptional?: boolean }).isOptional : true),
        invalid: !!errorMessage
    } as Record<string, unknown>);

    const FieldContainer = context.fieldContainerComponent;
    const FieldDecorator = context.fieldDecoratorComponent;
    const ErrorDisplay = context.errorDisplayComponent;
    const TooltipWrapper = context.tooltipComponent;

    // Prepare error display
    const errors = errorMessage ? [errorMessage] : [];
    const errorElement = context.showErrors && errors.length > 0 && (
        ErrorDisplay ? (
            <ErrorDisplay errors={errors} fieldName={propertyName} />
        ) : (
            <small 
                className={context.errorClassName || 'p-error'}
                style={{ display: 'block', marginTop: '0.25rem', color: 'var(--color-error, #c00)', fontSize: '0.875rem' }}
            >
                {errorMessage}
            </small>
        )
    );

    // Wrap field with decorator if icon or description exists
    let decoratedField = clonedField;
    if (fieldProps.icon || fieldProps.description) {
        if (FieldDecorator) {
            // When using a custom decorator with an icon, set hasLeftAddon on the field
            const fieldForDecorator = fieldProps.icon ? (
                React.cloneElement(clonedField as React.ReactElement, {
                    hasLeftAddon: true,
                    style: {
                        ...((clonedField as React.ReactElement).props?.style || {}),
                        flex: 1
                    }
                })
            ) : clonedField;
            decoratedField = (
                <FieldDecorator icon={fieldProps.icon} description={fieldProps.description}>
                    {fieldForDecorator}
                </FieldDecorator>
            );
        } else {
            // Default decoration: icon addon and tooltip
            const iconAddon = fieldProps.icon && (
                <span
                    className={context.iconAddonClassName || 'p-inputgroup-addon'}
                    style={{
                        display: 'flex',
                        alignItems: 'center',
                        padding: '0.75rem',
                        backgroundColor: 'var(--color-background-secondary)',
                        border: '1px solid var(--color-border)',
                        borderRight: 'none',
                        borderRadius: 'var(--radius-md) 0 0 var(--radius-md)'
                    }}
                >
                    {fieldProps.icon}
                </span>
            );

            // When there's an icon, set hasLeftAddon prop to remove left border-radius
            const fieldWithAdjustedStyle = fieldProps.icon ? (
                React.cloneElement(clonedField as React.ReactElement, {
                    hasLeftAddon: true,
                    style: {
                        ...((clonedField as React.ReactElement).props?.style || {}),
                        flex: 1
                    }
                })
            ) : clonedField;

            const wrappedField = (
                <div style={{ display: 'flex', width: '100%', alignItems: 'stretch' }}>
                    {iconAddon}
                    {fieldWithAdjustedStyle}
                </div>
            );

            decoratedField = fieldProps.description ? (
                TooltipWrapper ? (
                    <TooltipWrapper description={fieldProps.description}>
                        {wrappedField}
                    </TooltipWrapper>
                ) : (
                    <div title={fieldProps.description} style={{ cursor: 'help' }}>
                        {wrappedField}
                    </div>
                )
            ) : wrappedField;
        }
    }

    const fieldContent = (
        <>
            {context.showTitles && fieldProps.title && (
                <label
                    style={{
                        display: 'block',
                        marginBottom: '0.5rem',
                        fontWeight: 500,
                        color: 'var(--color-text)'
                    }}
                >
                    {fieldProps.title}
                </label>
            )}
            {decoratedField}
        </>
    );

    if (FieldContainer) {
        // FieldContainer handles error display through errorMessage prop
        return (
            <FieldContainer
                title={fieldProps.title}
                errorMessage={context.showErrors ? errorMessage : undefined}
            >
                {fieldContent}
            </FieldContainer>
        );
    }

    return (
        <div className="w-full" style={{ marginBottom: '1rem' }}>
            {fieldContent}
            {errorElement}
        </div>
    );
};

CommandFormFieldWrapper.displayName = 'CommandFormFieldWrapper';

export const CommandFormFields = (props: CommandFormFieldsProps) => {
    const { fields, columns, orderedChildren } = props;

    // Render columns if provided
    if (columns && columns.length > 0) {
        return (
            <div className="card flex flex-column md:flex-row gap-3">
                {columns.map((column, columnIndex) => (
                    <div key={`column-${columnIndex}`} className="flex flex-column gap-3 flex-1">
                        {column.fields.map((field, index) => {
                            const fieldProps = field.props as CommandFormFieldProps;
                            const propertyAccessor = fieldProps.value;
                            const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : `field-${columnIndex}-${index}`;

                            return (
                                <CommandFormFieldWrapper
                                    key={propertyName}
                                    field={field}
                                />
                            );
                        })}
                    </div>
                ))}
            </div>
        );
    }

    // If we have ordered children, use them to render in the correct order
    if (orderedChildren && orderedChildren.length > 0) {
        return (
            <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
                {orderedChildren.map((item) => {
                    if (item.type === 'field') {
                        const field = item.content as React.ReactElement<CommandFormFieldProps>;
                        const fieldProps = field.props as CommandFormFieldProps;
                        const propertyAccessor = fieldProps.value;
                        const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : `field-${item.index}`;

                        return (
                            <CommandFormFieldWrapper
                                key={propertyName}
                                field={field}
                            />
                        );
                    } else {
                        return (
                            <React.Fragment key={`other-${item.index}`}>
                                {item.content}
                            </React.Fragment>
                        );
                    }
                })}
            </div>
        );
    }

    // Fallback: Render fields only (single column layout)
    return (
        <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
            {(fields || []).map((field, index) => {
                const fieldProps = field.props as CommandFormFieldProps;
                const propertyAccessor = fieldProps.value;
                const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : `field-${index}`;

                return (
                    <CommandFormFieldWrapper
                        key={propertyName}
                        field={field}
                    />
                );
            })}
        </div>
    );
};

// Helper function to extract property name from accessor function
function getPropertyName<T>(accessor: (obj: T) => unknown): string {
    const fnStr = accessor.toString();
    const match = fnStr.match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);
    return match ? match[1] : '';
}

CommandFormFields.displayName = 'CommandFormFields';
