// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandFormFields, ColumnInfo, CommandFormFieldWrapper } from './CommandFormFields';
import { CommandFormContext, useCommandFormContext, type BeforeExecuteCallback, type CommandFormContextValue, type FieldValidationInfo, type FieldContainerProps, type FieldDecoratorProps, type ErrorDisplayProps, type TooltipWrapperProps } from './CommandFormContext';
import { Constructor } from '@cratis/fundamentals';
import { useCommand, SetCommandValues } from '../useCommand';
import { ICommandResult } from '@cratis/arc/commands';
import { Command } from '@cratis/arc/commands';
import React, { useMemo, useState, useCallback } from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import { getPropertyNameFromAccessor } from './getPropertyNameFromAccessor';

// Re-export for backwards compatibility
export { useCommandFormContext } from './CommandFormContext';

export interface CommandFormProps<TCommand extends object> {
    command: Constructor<TCommand>;
    initialValues?: Partial<TCommand>;
    currentValues?: Partial<TCommand> | undefined;
    onFieldValidate?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown) => string | undefined;
    onFieldChange?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown, validationInfo?: FieldValidationInfo) => void;
    onBeforeExecute?: BeforeExecuteCallback<TCommand>;
    showTitles?: boolean;
    showErrors?: boolean;
    validateOn?: 'blur' | 'change' | 'both';
    validateAllFieldsOnChange?: boolean;
    validateOnInit?: boolean;
    autoServerValidate?: boolean;
    autoServerValidateThrottle?: number;
    fieldContainerComponent?: React.ComponentType<FieldContainerProps>;
    fieldDecoratorComponent?: React.ComponentType<FieldDecoratorProps>;
    errorDisplayComponent?: React.ComponentType<ErrorDisplayProps>;
    tooltipComponent?: React.ComponentType<TooltipWrapperProps>;
    errorClassName?: string;
    iconAddonClassName?: string;
    children?: React.ReactNode;
}

// Hook to get just the command instance for easier access
export const useCommandInstance = <TCommand = unknown>() => {
    const { commandInstance } = useCommandFormContext<TCommand>();
    return commandInstance as TCommand;
};

// Hook to get setCommandResult for easier access
export const useSetCommandResult = () => {
    const { setCommandResult } = useCommandFormContext();
    return setCommandResult;
};

const CommandFormFieldsWrapper = (props: { children: React.ReactNode }) => {
    React.Children.forEach(props.children, child => {
        if (React.isValidElement(child)) {
            const component = child.type as React.ComponentType<unknown>;
            if (component.displayName !== 'CommandFormField') {
                throw new Error(`Only CommandFormField components are allowed as children of CommandForm.Fields. Got: ${component.displayName || component.name || 'Unknown'}`);
            }
        }
    });

    return <></>;
};

CommandFormFieldsWrapper.displayName = 'CommandFormFieldsWrapper';

const getCommandFormFields = <TCommand,>(props: { children?: React.ReactNode }): { fieldsOrColumns: React.ReactElement[] | ColumnInfo[], otherChildren: React.ReactNode[], initialValuesFromFields: Partial<TCommand>, orderedChildren: Array<{ type: 'field' | 'other', content: React.ReactNode, index: number }> } => {
    if (!props.children) {
        return { fieldsOrColumns: [], otherChildren: [], initialValuesFromFields: {}, orderedChildren: [] };
    }
    let fields: React.ReactElement<CommandFormFieldProps>[] = [];
    const columns: ColumnInfo[] = [];
    let hasColumns = false;
    const otherChildren: React.ReactNode[] = [];
    const orderedChildren: Array<{ type: 'field' | 'other', content: React.ReactNode, index: number }> = [];
    let fieldIndex = 0;
    let otherIndex = 0;
    let initialValuesFromFields: Partial<TCommand> = {};

    const extractInitialValue = (field: React.ReactElement) => {
        const fieldProps = field.props as Record<string, unknown>;
        if (fieldProps.currentValue !== undefined && fieldProps.value) {
            const propertyAccessor = fieldProps.value;
            const propertyName = getPropertyNameFromAccessor(propertyAccessor);
            if (propertyName) {
                initialValuesFromFields = { ...initialValuesFromFields, [propertyName]: fieldProps.currentValue } as Partial<TCommand>;
            }
        }
    };

    React.Children.toArray(props.children).forEach(child => {
        if (!React.isValidElement(child)) {
            otherChildren.push(child);
            orderedChildren.push({ type: 'other', content: child, index: otherIndex++ });
            return;
        }

        const component = child.type as React.ComponentType<unknown>;

        // Check if child is a CommandFormColumn
        if (component.displayName === 'CommandFormColumn') {
            hasColumns = true;
            const childProps = child.props as { children?: React.ReactNode };
            const columnFields = React.Children.toArray(childProps.children).filter(child => {
                if (React.isValidElement(child)) {
                    const comp = child.type as React.ComponentType<unknown>;
                    if (comp.displayName === 'CommandFormField') {
                        extractInitialValue(child as React.ReactElement);
                        return true;
                    }
                }
                return false;
            }) as React.ReactElement[];
            columns.push({ fields: columnFields as React.ReactElement<CommandFormFieldProps>[] });
        }
        // Check if child is a CommandFormField (direct child)
        else if (component.displayName === 'CommandFormField') {
            extractInitialValue(child as React.ReactElement);
            fields.push(child as React.ReactElement<CommandFormFieldProps>);
            orderedChildren.push({ type: 'field', content: child, index: fieldIndex++ });
        }
        // Check if child is Fields wrapper (backwards compatibility)
        else if (component === CommandFormFieldsWrapper || component.displayName === 'CommandFormFieldsWrapper') {
            const childProps = child.props as { children: React.ReactNode };
            const relevantChildren = React.Children.toArray(childProps.children).filter(child => {
                if (React.isValidElement(child)) {
                    const component = child.type as React.ComponentType<unknown>;
                    if (component.displayName === 'CommandFormField') {
                        extractInitialValue(child as React.ReactElement);
                        return true;
                    }
                }
                return false;
            }) as React.ReactElement[];
            fields = [...fields, ...(relevantChildren as React.ReactElement<CommandFormFieldProps>[])];
        }
        // Everything else is not a field, keep it as other children
        else {
            otherChildren.push(child);
            orderedChildren.push({ type: 'other', content: child, index: otherIndex++ });
        }
    });

    return { fieldsOrColumns: hasColumns ? columns : fields, otherChildren, initialValuesFromFields, orderedChildren };
};

const CommandFormComponent = <TCommand extends object = object>(props: CommandFormProps<TCommand>) => {
    const { fieldsOrColumns, initialValuesFromFields, orderedChildren } = useMemo(() => getCommandFormFields<TCommand>(props), [props.children]);

    // Extract matching properties from currentValues
    const valuesFromCurrentValues = useMemo(() => {
        if (!props.currentValues) return {};

        const tempCommand = new props.command();
        const commandProperties = ((tempCommand as Record<string, unknown>).properties || []) as string[];
        const extracted: Partial<TCommand> = {};

        commandProperties.forEach((propertyName: string) => {
            if ((props.currentValues as Record<string, unknown>)[propertyName] !== undefined) {
                (extracted as Record<string, unknown>)[propertyName] = (props.currentValues as Record<string, unknown>)[propertyName];
            }
        });

        return extracted;
    }, [props.currentValues, props.command]);

    // Merge initialValues prop with values extracted from field currentValue props and currentValues
    const mergedInitialValues = useMemo(() => ({
        ...valuesFromCurrentValues,
        ...initialValuesFromFields,
        ...props.initialValues
    }), [valuesFromCurrentValues, initialValuesFromFields, props.initialValues]);

    // useCommand returns [instance, setter, clearer, version] for the typed command. Provide generics so commandInstance is TCommand.
    // Using type assertion through unknown to work around generic constraint mismatch
    const useCommandResult = useCommand(props.command as unknown as Constructor<Command<Partial<TCommand>, object>>, mergedInitialValues);
    const commandInstance = useCommandResult[0] as unknown as TCommand;
    const setCommandValues = useCommandResult[1] as SetCommandValues<TCommand>;
    const commandVersion = useCommandResult[3];
    const [commandResult, setCommandResult] = useState<ICommandResult<unknown> | undefined>(undefined);
    const [fieldValidities, setFieldValidities] = useState<Record<string, boolean>>({});
    const [customFieldErrors, setCustomFieldErrors] = useState<Record<string, string>>({});
    const initializedRef = React.useRef(false);
    const lastServerValidateVersion = React.useRef<number>(-1);
    const serverValidateThrottleTimer = React.useRef<NodeJS.Timeout | null>(null);

    // Update command values when mergedInitialValues changes (e.g., when data loads asynchronously)
    // When using currentValues, always update when they change (reactive mode for editing)
    // When using initialValues only, set values once on mount
    React.useEffect(() => {
        const hasCurrentValues = props.currentValues !== undefined && props.currentValues !== null;
        
        if (hasCurrentValues) {
            // Reactive mode: update whenever currentValues changes
            if (mergedInitialValues && Object.keys(mergedInitialValues).length > 0) {
                setCommandValues(mergedInitialValues as TCommand);
            }
        } else if (!initializedRef.current && mergedInitialValues && Object.keys(mergedInitialValues).length > 0) {
            // Static mode: set values only once on initialization
            setCommandValues(mergedInitialValues as TCommand);
        }

        // Validate on init if requested (only on first initialization)
        if (!initializedRef.current && props.validateOnInit && commandInstance && typeof (commandInstance as Record<string, unknown>).validate === 'function') {
            initializedRef.current = true;
            void (async () => {
                const validationResult = await ((commandInstance as Record<string, unknown>).validate as () => Promise<ICommandResult<unknown>>)();
                if (validationResult) {
                    setCommandResult(validationResult);
                }
            })();
        } else if (!initializedRef.current) {
            initializedRef.current = true;
        }
    }, [mergedInitialValues, props.validateOnInit, props.currentValues, commandInstance, setCommandValues]);

    const isValid = Object.values(fieldValidities).every(valid => valid);

    // Auto server validate when all client validations pass
    React.useEffect(() => {
        if (!props.autoServerValidate) {
            return;
        }

        // Clear any pending throttle timer
        if (serverValidateThrottleTimer.current) {
            clearTimeout(serverValidateThrottleTimer.current);
            serverValidateThrottleTimer.current = null;
        }

        // Only call server validate if all fields are valid (client validation passed)
        const allFieldsValid = Object.keys(fieldValidities).length > 0 && isValid;
        
        // Check if we've already validated this command version
        const alreadyValidatedThisVersion = lastServerValidateVersion.current === commandVersion;
        
        // Must have all fields valid and not already validated this version
        if (allFieldsValid && !alreadyValidatedThisVersion && commandInstance && typeof (commandInstance as Record<string, unknown>).validate === 'function') {
            const performValidation = () => {
                lastServerValidateVersion.current = commandVersion;
                void (async () => {
                    try {
                        const validationResult = await ((commandInstance as Record<string, unknown>).validate as () => Promise<ICommandResult<unknown>>)();
                        if (validationResult) {
                            setCommandResult(validationResult);
                        }
                    } catch (error) {
                        // Silently handle validation errors - they'll be in the result
                        console.error('Server validation error:', error);
                    }
                })();
            };

            // Apply throttle if specified
            const throttleMs = props.autoServerValidateThrottle ?? 500;
            if (throttleMs > 0) {
                serverValidateThrottleTimer.current = setTimeout(performValidation, throttleMs);
            } else {
                performValidation();
            }
        }

        return () => {
            if (serverValidateThrottleTimer.current) {
                clearTimeout(serverValidateThrottleTimer.current);
                serverValidateThrottleTimer.current = null;
            }
        };
    }, [props.autoServerValidate, props.autoServerValidateThrottle, commandInstance, commandVersion, isValid, fieldValidities]);

    const setFieldValidity = useCallback((fieldName: string, isFieldValid: boolean) => {
        setFieldValidities(prev => ({ ...prev, [fieldName]: isFieldValid }));
    }, []);

    const setCustomFieldError = useCallback((fieldName: string, error: string | undefined) => {
        setCustomFieldErrors(prev => {
            if (error === undefined) {
                const newErrors = { ...prev };
                delete newErrors[fieldName];
                return newErrors;
            }
            return { ...prev, [fieldName]: error };
        });
    }, []);

    const getFieldError = (propertyName: string): string | undefined => {
        // Check custom field errors first
        if (customFieldErrors[propertyName]) {
            return customFieldErrors[propertyName];
        }

        if (!commandResult || !commandResult.validationResults) {
            return undefined;
        }

        for (const validationResult of commandResult.validationResults) {
            if (validationResult.members && validationResult.members.includes(propertyName)) {
                return validationResult.message;
            }
        }

        return undefined;
    };

    const handleExecute = useCallback(async (): Promise<ICommandResult<unknown>> => {
        let finalValues = commandInstance;

        // Apply onBeforeExecute transformation if provided
        if (props.onBeforeExecute) {
            finalValues = props.onBeforeExecute(commandInstance);
            setCommandValues(finalValues);
        }

        // Execute the command
        if (typeof (finalValues as unknown as Command).execute === 'function') {
            const result = await (finalValues as unknown as Command).execute();
            setCommandResult(result);
            return result;
        }

        throw new Error('Command instance does not have an execute method');
    }, [commandInstance, props.onBeforeExecute, setCommandValues, setCommandResult]);

    const handleFormSubmit = useCallback((e: React.FormEvent) => {
        e.preventDefault();
        e.stopPropagation();
        void handleExecute();
    }, [handleExecute]);

    const exceptionMessages = commandResult?.exceptionMessages || [];
    const hasColumns = fieldsOrColumns.length > 0 && 'fields' in fieldsOrColumns[0];

    const contextValue: CommandFormContextValue<TCommand> = {
        command: props.command,
        commandInstance,
        commandVersion,
        setCommandValues,
        commandResult,
        setCommandResult,
        getFieldError,
        isValid,
        setFieldValidity,
        onFieldValidate: props.onFieldValidate,
        onFieldChange: props.onFieldChange,
        onBeforeExecute: props.onBeforeExecute,
        onExecute: handleExecute,
        customFieldErrors,
        setCustomFieldError,
        showTitles: props.showTitles ?? true,
        showErrors: props.showErrors ?? true,
        validateOn: props.validateOn ?? 'blur',
        validateAllFieldsOnChange: props.validateAllFieldsOnChange ?? false,
        validateOnInit: props.validateOnInit ?? false,
        autoServerValidate: props.autoServerValidate ?? false,
        autoServerValidateThrottle: props.autoServerValidateThrottle ?? 500,
        fieldContainerComponent: props.fieldContainerComponent,
        fieldDecoratorComponent: props.fieldDecoratorComponent,
        errorDisplayComponent: props.errorDisplayComponent,
        tooltipComponent: props.tooltipComponent,
        errorClassName: props.errorClassName,
        iconAddonClassName: props.iconAddonClassName
    };

    return (
        <CommandFormContext.Provider value={contextValue as CommandFormContextValue<unknown>}>
            <form onSubmit={handleFormSubmit} noValidate>
                <CommandFormFields 
                    fields={hasColumns ? undefined : (fieldsOrColumns as React.ReactElement<CommandFormFieldProps>[])} 
                    columns={hasColumns ? fieldsOrColumns as ColumnInfo[] : undefined}
                    orderedChildren={orderedChildren}
                />
                {exceptionMessages.length > 0 && (
                    <div style={{ marginTop: '1rem', padding: '1rem', border: '1px solid var(--color-border)', borderRadius: 'var(--radius-md)', backgroundColor: 'var(--color-error-bg, #fee)' }}>
                        <h4 style={{ margin: '0 0 0.5rem 0', fontSize: '1rem', fontWeight: 600, color: 'var(--color-error, #c00)' }}>The server responded with</h4>
                        <ul style={{ margin: 0, paddingLeft: '1.5rem' }}>
                            {exceptionMessages.map((msg, idx) => (
                                <li key={idx}>{msg}</li>
                            ))}
                        </ul>
                    </div>
                )}
            </form>
        </CommandFormContext.Provider>
    );
};

interface CommandFormColumnProps {
    children: React.ReactNode;
}

const CommandFormColumnComponent = (props: CommandFormColumnProps) => {
    const children = React.Children.toArray(props.children);

    return (
        <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
            {children.map((child, index) => {
                if (React.isValidElement(child)) {
                    const component = child.type as React.ComponentType<unknown>;
                    if (component.displayName === 'CommandFormField') {
                        return (
                            <CommandFormFieldWrapper
                                key={`column-field-${index}`}
                                field={child as React.ReactElement}
                            />
                        );
                    }
                }
                // Render non-field children as-is
                return <React.Fragment key={`column-other-${index}`}>{child}</React.Fragment>;
            })}
        </div>
    );
};

CommandFormColumnComponent.displayName = 'CommandFormColumn';

CommandFormComponent.Fields = CommandFormFieldsWrapper;
CommandFormComponent.Column = CommandFormColumnComponent;

export const CommandForm = CommandFormComponent;
