// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCommandFormContext, type FieldValidationInfo } from './CommandFormContext';
import { useCommandFormFieldRegistration } from './CommandFormFieldRegistrationContext';
import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import type { ICommandResult } from '@cratis/arc/commands';
import { memberMatchesField } from './memberMatchesField';
import { isCommandFormColumn } from './commandFormMarkers';
import { renderCommandFormDescendants } from './renderCommandFormDescendants';
import { runCommandValidation } from './runCommandValidation';
import { shouldEmitCommandFormDevelopmentWarnings } from './commandFormRuntime';

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

// Separate component for each field to prevent re-rendering all fields
const CommandFormFieldWrapper = ({
    field,
}: {
    field: React.ReactElement<CommandFormFieldProps>;
}) => {
    useCommandFormFieldRegistration(field);
    const context = useCommandFormContext<unknown>();
    const fieldProps = field.props as CommandFormFieldProps;
    const propertyAccessor = fieldProps.value;

    // Get the property name from the accessor function
    const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : '';
    const hasWarnedAboutInvalidBinding = React.useRef(false);

    React.useEffect(() => {
        if (
            shouldEmitCommandFormDevelopmentWarnings() &&
            !propertyName &&
            !hasWarnedAboutInvalidBinding.current
        ) {
            hasWarnedAboutInvalidBinding.current = true;
            const component = field.type as React.ComponentType<unknown> & {
                displayName?: string;
                commandFormFieldName?: string;
            };
            const componentName =
                component.commandFormFieldName ||
                component.displayName ||
                component.name ||
                'CommandFormField';
            console.warn(
                `CommandForm field '${componentName}' could not resolve a command property from its value accessor and remains unbound. ` +
                    'Pass an accessor such as `command => command.property`.',
            );
        }
    }, [field.type, propertyName]);

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
    const propertyDescriptor =
        propertyName &&
        (context.commandInstance as Record<string, unknown>)?.propertyDescriptors
            ? (
                  (context.commandInstance as Record<string, unknown>)
                      .propertyDescriptors as Array<Record<string, unknown>>
              ).find((pd: Record<string, unknown>) => pd.name === propertyName)
            : undefined;

    // Clone the field element with the current value and onChange handler
    const clonedField = React.cloneElement(
        field as React.ReactElement,
        {
            ...fieldProps,
            currentValue,
            propertyDescriptor,
            fieldName: propertyName,
            onValueChange: async (value: unknown) => {
                if (propertyName) {
                    const oldValue = currentValue;

                    // Update the command value (mutates command.current in-place immediately —
                    // useCommand uses a useRef so validate() sees the new value right away).
                    context.setCommandValues({ [propertyName]: value } as Record<
                        string,
                        unknown
                    >);

                    // Call custom field validator SYNCHRONOUSLY — user-provided logic, no async.
                    // This runs before validate() so the result is available immediately (no await).
                    if (context.onFieldValidate) {
                        const validationError = context.onFieldValidate(
                            context.commandInstance as Record<string, unknown>,
                            propertyName,
                            oldValue,
                            value,
                        );
                        context.setCustomFieldError(propertyName, validationError);
                    }

                    // Call field change callback SYNCHRONOUSLY — fired on every value change.
                    // Uses previously computed errors for immediate validationInfo; the full
                    // silent validation result updates asynchronously below.
                    if (context.onFieldChange) {
                        const prevErrors =
                            context.commandResult?.validationResults
                                ?.filter((vr) =>
                                    memberMatchesField(vr.members, propertyName),
                                )
                                .map((vr) => vr.message) || [];
                        const validationInfo: FieldValidationInfo = {
                            isValid: prevErrors.length === 0,
                            errors: prevErrors,
                        };
                        context.onFieldChange(
                            context.commandInstance as Record<string, unknown>,
                            propertyName,
                            oldValue,
                            value,
                            validationInfo,
                        );
                    }

                    // Always run silent validation after every value change. This is the immediate driver of
                    // context.isValid — it runs regardless of validateOn so isValid is always accurate.
                    //
                    // Client-side only, deliberately. This fires once per keystroke, and asking the server that
                    // often is a round trip per character with nothing to damp it — autoServerValidateThrottle
                    // governs the effect in CommandForm, not this call. That effect is the single server path:
                    // it debounces behind the throttle and feeds its result back into the same silent state, so
                    // the server still has the final say on isValid, just once the typing stops rather than
                    // once per character.
                    // Claimed before the run starts, so a burst of keystrokes resolving out of order applies
                    // the newest verdict rather than the last one to arrive - see beginSilentValidation.
                    const issue = context.beginSilentValidation();
                    const validationResult = await runCommandValidation(
                        context.commandInstance,
                        false,
                    );
                    const describesCurrentValues =
                        validationResult !== undefined &&
                        context.setSilentValidationResult(validationResult, issue);

                    // Show validation error messages based on the validateOn setting.
                    // This is purely a display concern and does NOT affect isValid, but a message describing
                    // values the field no longer holds is as wrong on screen as it is in isValid.
                    const shouldValidateOnChange =
                        context.validateOn === 'change' || context.validateOn === 'both';
                    if (
                        shouldValidateOnChange &&
                        describesCurrentValues &&
                        validationResult
                    ) {
                        if (context.validateAllFieldsOnChange) {
                            context.setCommandResult(validationResult);
                        } else {
                            // Per-field merge: keep errors from untouched fields, update this field.
                            const currentErrors =
                                context.commandResult?.validationResults || [];
                            const errorsFromOtherFields = currentErrors.filter(
                                (vr) => !memberMatchesField(vr.members, propertyName),
                            );
                            const errorsForThisField =
                                validationResult.validationResults?.filter((vr) =>
                                    memberMatchesField(vr.members, propertyName),
                                ) || [];
                            const mergedValidationResults = [
                                ...errorsFromOtherFields,
                                ...errorsForThisField,
                            ];
                            context.setCommandResult({
                                ...validationResult,
                                validationResults: mergedValidationResults,
                                isValid: mergedValidationResults.length === 0,
                            });
                        }
                    }
                }
                fieldProps.onChange?.(value as unknown);
            },
            onBlur: async () => {
                if (propertyName) {
                    const shouldValidateOnBlur =
                        context.validateOn === 'blur' || context.validateOn === 'both';

                    let validationResult: ICommandResult<unknown> | undefined;
                    if (shouldValidateOnBlur) {
                        // With autoServerValidate this run crosses the network, which makes it the one most
                        // likely to be overtaken by a client-side run issued after it.
                        const issue = context.beginSilentValidation();
                        validationResult = await runCommandValidation(
                            context.commandInstance,
                            context.autoServerValidate,
                        );

                        // Keep silent result current (covers edge cases where blur fires
                        // without a preceding onChange, e.g. clipboard paste in some browsers).
                        if (
                            validationResult &&
                            context.setSilentValidationResult(validationResult, issue)
                        ) {
                            if (context.validateAllFieldsOnChange) {
                                context.setCommandResult(validationResult);
                            } else {
                                // Per-field merge: keep errors from untouched fields, update this field.
                                const currentErrors =
                                    context.commandResult?.validationResults || [];
                                const errorsFromOtherFields = currentErrors.filter(
                                    (vr) => !memberMatchesField(vr.members, propertyName),
                                );
                                const errorsForThisField =
                                    validationResult.validationResults?.filter((vr) =>
                                        memberMatchesField(vr.members, propertyName),
                                    ) || [];
                                const mergedValidationResults = [
                                    ...errorsFromOtherFields,
                                    ...errorsForThisField,
                                ];
                                context.setCommandResult({
                                    ...validationResult,
                                    validationResults: mergedValidationResults,
                                    isValid: mergedValidationResults.length === 0,
                                });
                            }
                        }
                    }

                    // Call field change callback if provided
                    if (context.onFieldChange && validationResult) {
                        const currentValue = (
                            context.commandInstance as Record<string, unknown>
                        )[propertyName];
                        const fieldErrors =
                            validationResult?.validationResults
                                ?.filter((vr) =>
                                    memberMatchesField(vr.members, propertyName),
                                )
                                .map((vr) => vr.message) || [];
                        const validationInfo: FieldValidationInfo = {
                            isValid: fieldErrors.length === 0,
                            errors: fieldErrors,
                        };
                        context.onFieldChange(
                            context.commandInstance as Record<string, unknown>,
                            propertyName,
                            currentValue,
                            currentValue,
                            validationInfo,
                        );
                    }
                }
            },
            required:
                fieldProps.required ??
                (propertyDescriptor
                    ? !(propertyDescriptor as { isOptional?: boolean }).isOptional
                    : true),
            invalid: !!errorMessage,
        } as Record<string, unknown>,
    );

    const FieldContainer = context.fieldContainerComponent;
    const FieldDecorator = context.fieldDecoratorComponent;
    const ErrorDisplay = context.errorDisplayComponent;
    const TooltipWrapper = context.tooltipComponent;

    // Prepare error display
    const errors = errorMessage ? [errorMessage] : [];
    const errorElement =
        context.showErrors &&
        errors.length > 0 &&
        (ErrorDisplay ? (
            <ErrorDisplay errors={errors} fieldName={propertyName} />
        ) : (
            <small
                className={context.errorClassName || 'p-error'}
                style={{
                    display: 'block',
                    marginTop: '0.25rem',
                    color: 'var(--color-error, #c00)',
                    fontSize: '0.875rem',
                }}
            >
                {errorMessage}
            </small>
        ));

    // Wrap field with decorator if icon or description exists
    let decoratedField = clonedField;
    if (fieldProps.icon || fieldProps.description) {
        if (FieldDecorator) {
            // When using a custom decorator with an icon, set hasLeftAddon on the field
            const fieldForDecorator = fieldProps.icon
                ? React.cloneElement(clonedField as React.ReactElement, {
                      hasLeftAddon: true,
                      style: {
                          ...((clonedField as React.ReactElement).props?.style || {}),
                          flex: 1,
                      },
                  })
                : clonedField;
            decoratedField = (
                <FieldDecorator
                    icon={fieldProps.icon}
                    description={fieldProps.description}
                >
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
                        borderRadius: 'var(--radius-md) 0 0 var(--radius-md)',
                    }}
                >
                    {fieldProps.icon}
                </span>
            );

            // When there's an icon, set hasLeftAddon prop to remove left border-radius
            const fieldWithAdjustedStyle = fieldProps.icon
                ? React.cloneElement(clonedField as React.ReactElement, {
                      hasLeftAddon: true,
                      style: {
                          ...((clonedField as React.ReactElement).props?.style || {}),
                          flex: 1,
                      },
                  })
                : clonedField;

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
            ) : (
                wrappedField
            );
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
                        color: 'var(--color-text)',
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
        <div className='w-full' style={{ marginBottom: '1rem' }}>
            {fieldContent}
            {errorElement}
        </div>
    );
};

CommandFormFieldWrapper.displayName = 'CommandFormFieldWrapper';

const isColumnElement = (
    node: React.ReactNode,
): node is React.ReactElement<{ children?: React.ReactNode }> =>
    React.isValidElement(node) &&
    isCommandFormColumn(node.type as React.ComponentType<unknown>);

const getNodeKey = (node: React.ReactNode, fallback: string): string =>
    React.isValidElement(node) && node.key !== null ? String(node.key) : fallback;

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
        const hasDirectColumns = orderedChildren.some((item) =>
            isColumnElement(item.content),
        );
        if (hasDirectColumns) {
            const containsOnlyColumns = orderedChildren.every((item) =>
                isColumnElement(item.content),
            );
            const className = containsOnlyColumns
                ? 'card flex flex-column md:flex-row gap-3'
                : 'card flex flex-column md:flex-row gap-3 flex-wrap';
            return (
                <div className={className}>
                    {orderedChildren.map((item) => {
                        const key = String(
                            item.key ??
                                getNodeKey(item.content, `${item.type}-${item.index}`),
                        );
                        if (isColumnElement(item.content)) {
                            return (
                                <div key={key} className='flex flex-column gap-3 flex-1'>
                                    {renderCommandFormDescendants(
                                        item.content.props.children,
                                        renderField,
                                    )}
                                </div>
                            );
                        }
                        return (
                            <div
                                key={key}
                                className='w-full'
                                style={{ flexBasis: '100%', width: '100%' }}
                            >
                                {renderCommandFormDescendants(item.content, renderField)}
                            </div>
                        );
                    })}
                </div>
            );
        }

        return (
            <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
                {orderedChildren.map((item) => (
                    <React.Fragment
                        key={String(
                            item.key ??
                                getNodeKey(item.content, `${item.type}-${item.index}`),
                        )}
                    >
                        {renderCommandFormDescendants(item.content, renderField)}
                    </React.Fragment>
                ))}
            </div>
        );
    }

    // Fallback: Render fields only (single column layout)
    return (
        <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
            {(fields || []).map((field, index) => {
                const fieldProps = field.props as CommandFormFieldProps;
                const propertyAccessor = fieldProps.value;
                const propertyName = propertyAccessor
                    ? getPropertyName(propertyAccessor)
                    : `field-${index}`;

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

// Helper function to extract property name from accessor function
function getPropertyName<T>(accessor: (obj: T) => unknown): string {
    const fnStr = accessor.toString();
    const match = fnStr.match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);
    return match ? match[1] : '';
}

CommandFormFields.displayName = 'CommandFormFields';

export { CommandFormFieldWrapper };
