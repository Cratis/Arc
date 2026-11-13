// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCommandFormContext } from './CommandFormContext.js';
import { useCommandFormFieldRegistration } from './CommandFormFieldRegistrationContext.js';
import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField.js';
import { ValidationResultSeverity } from '@cratis/arc/validation';
import type { CommandFormMarked } from './commandFormMarkers.js';
import { renderCommandFormDescendants } from './renderCommandFormDescendants.js';
import { shouldEmitCommandFormDevelopmentWarnings } from './commandFormRuntime.js';
import { CommandFormNativeResultContext } from './CommandFormNativeResultContext.js';
import { createFieldBlurHandler, createFieldValueChangeHandler } from './commandFormFieldHandlers.js';
import { decorateCommandFormField } from './decorateCommandFormField.js';
import {
    FieldContent,
    FieldError,
    findPropertyDescriptor,
    getPropertyName,
    isFieldRequired,
    warnAboutUnboundField,
} from './commandFormFieldParts.js';
import {
    hasDirectColumns,
    OrderedColumnsLayout,
    OrderedStackLayout,
} from './CommandFormFieldsLayouts.js';

export interface ColumnInfo {
    fields: React.ReactElement<CommandFormFieldProps>[];
}

export interface CommandFormFieldsProps {
    fields?: React.ReactElement<CommandFormFieldProps>[];
    columns?: ColumnInfo[];
    orderedChildren?: Array<{
        type: 'field' | 'other';
        content: React.ReactNode;
        index: number;
        key?: React.Key;
    }>;
}

/**
 * Resolves the command property a field is bound to and registers the field with the form.
 * An explicit fieldName wins; a dynamic accessor such as `instance => instance[name]` cannot be
 * resolved from source text, so inference is only a fallback for ordinary member accessors.
 */
const useFieldPropertyName = (
    field: React.ReactElement<CommandFormFieldProps>,
    fieldProps: CommandFormFieldProps,
): string => {
    const propertyAccessor = fieldProps.value;
    const propertyName =
        fieldProps.fieldName || (propertyAccessor ? getPropertyName(propertyAccessor) : '');
    useCommandFormFieldRegistration(fieldProps, propertyName);
    const hasWarnedAboutInvalidBinding = React.useRef(false);

    React.useEffect(() => {
        const shouldWarn =
            !propertyName &&
            !hasWarnedAboutInvalidBinding.current &&
            shouldEmitCommandFormDevelopmentWarnings();
        if (shouldWarn) {
            hasWarnedAboutInvalidBinding.current = true;
            warnAboutUnboundField(field);
        }
    }, [field.type, propertyName]);

    return propertyName;
};

// Separate component for each field to prevent re-rendering all fields
const CommandFormFieldWrapper = ({
    field,
}: {
    field: React.ReactElement<CommandFormFieldProps>;
}) => {
    const context = useCommandFormContext<unknown>();
    const nativeResultContext = React.useContext(CommandFormNativeResultContext);
    const nativeCommandResult = nativeResultContext ? nativeResultContext.result : context.commandResult;
    const policy = (context.commandInstance as { blockOnValidationSeverity?: ValidationResultSeverity } | undefined)?.blockOnValidationSeverity;
    const fieldProps = field.props as CommandFormFieldProps;
    const generatedId = React.useId();
    const fieldId = fieldProps.id ?? generatedId;
    const groupRole = (field.type as CommandFormMarked).commandFormFieldGroupRole;
    const propertyName = useFieldPropertyName(field, fieldProps);

    // Get the current value from the command instance
    // commandVersion ensures this component re-renders when values change
    const currentValue = React.useMemo(() => {
        return propertyName
            ? (context.commandInstance as Record<string, unknown>)?.[propertyName]
            : undefined;
    }, [context.commandInstance, propertyName, context.commandVersion]);

    // Get the error message for this field, if any
    const errorMessage = propertyName ? context.getFieldError(propertyName) : undefined;

    // Get the property descriptor for this field from the command instance
    const propertyDescriptor = findPropertyDescriptor(context.commandInstance, propertyName);

    const handlerScope = { context, propertyName, policy, nativeCommandResult, fieldProps, currentValue };

    // Clone the field element with the current value and onChange handler
    const clonedField = React.cloneElement(
        field as React.ReactElement,
        {
            ...fieldProps,
            id: fieldId,
            currentValue,
            propertyDescriptor,
            fieldName: propertyName,
            onValueChange: createFieldValueChangeHandler(handlerScope),
            onBlur: createFieldBlurHandler(handlerScope),
            required: isFieldRequired(fieldProps, propertyDescriptor),
            invalid: !!errorMessage,
        } as Record<string, unknown>,
    );

    const FieldContainer = context.fieldContainerComponent;
    const fieldContent = (
        <FieldContent
            fieldProps={fieldProps}
            fieldId={fieldId}
            groupRole={groupRole}
            showTitles={context.showTitles}
            decoratedField={decorateCommandFormField(clonedField, fieldProps, context)}
        />
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
        <div className='w-full' style={{ marginBottom: '1rem' }}>
            {fieldContent}
            <FieldError context={context} errorMessage={errorMessage} propertyName={propertyName} />
        </div>
    );
};

CommandFormFieldWrapper.displayName = 'CommandFormFieldWrapper';

export const CommandFormFields = (props: CommandFormFieldsProps) => {
    const { fields, columns, orderedChildren } = props;

    const renderField = (field: React.ReactElement<CommandFormFieldProps>) => (
        <CommandFormFieldWrapper field={field} />
    );

    // Render columns if provided
    if (columns && columns.length > 0) {
        return (
            <div className='card flex flex-column md:flex-row gap-3'>
                {columns.map((column, columnIndex) => (
                    <div
                        key={`column-${columnIndex}`}
                        className='flex flex-column gap-3 flex-1'
                    >
                        {renderCommandFormDescendants(column.fields, renderField)}
                    </div>
                ))}
            </div>
        );
    }

    if (orderedChildren && orderedChildren.length > 0) {
        return hasDirectColumns(orderedChildren)
            ? <OrderedColumnsLayout orderedChildren={orderedChildren} renderField={renderField} />
            : <OrderedStackLayout orderedChildren={orderedChildren} renderField={renderField} />;
    }

    // Fallback: Render fields only (single column layout)
    return (
        <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
            {(fields || []).map((field, index) => {
                const fieldProps = field.props as CommandFormFieldProps;
                const propertyAccessor = fieldProps.value;
                const propertyName =
                    fieldProps.fieldName ||
                    (propertyAccessor ? getPropertyName(propertyAccessor) : `field-${index}`);

                return (
                    <CommandFormFieldWrapper
                        key={`${propertyName}-${index}`}
                        field={field}
                    />
                );
            })}
        </div>
    );
};

CommandFormFields.displayName = 'CommandFormFields';

export { CommandFormFieldWrapper };
