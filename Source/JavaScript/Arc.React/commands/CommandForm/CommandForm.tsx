// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandFormFields, ColumnInfo, CommandFormFieldWrapper } from './CommandFormFields';
import { CommandFormContext, useCommandFormContext, type BeforeExecuteCallback, type CommandFormContextValue, type FieldValidationInfo, type FieldContainerProps, type FieldDecoratorProps, type ErrorDisplayProps, type TooltipWrapperProps } from './CommandFormContext';
import { Constructor } from '@cratis/fundamentals';
import { useCommand, SetCommandValues } from '../useCommand';
import { ICommandResult } from '@cratis/arc/commands';
import { Command } from '@cratis/arc/commands';
import { ValidationResult } from '@cratis/arc/validation';
import React, { useMemo, useState, useCallback, useImperativeHandle } from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import { getPropertyNameFromAccessor } from './getPropertyNameFromAccessor';
import { memberMatchesField } from './memberMatchesField';
import { runCommandValidation } from './runCommandValidation';
import { useIdentity } from '../../identity';
import { isCommandFormColumn, isCommandFormField, markAsCommandFormColumn } from './commandFormMarkers';
import { withoutUndefinedValues } from './withoutUndefinedValues';

// Re-export for backwards compatibility
export { useCommandFormContext } from './CommandFormContext';

export interface CommandFormProps<TCommand extends object, TResponse = object> {
    command: Constructor<TCommand>;

    /**
     * The synchronous baseline the form starts from, and what change tracking measures against.
     *
     * This is a *seed*: it supplies values, it does not clear them. A key holding `undefined`
     * supplies nothing, so whatever {@link CommandFormProps.currentValues} resolved - or the command
     * class's own default - stays in place. That matters because these values are usually written
     * out by the caller as a fixed object literal, where a property that has no value yet reads as
     * `undefined` on every render, including the ones after an asynchronous lookup has answered.
     * A field's `currentValue` prop is the same layer and follows the same rule.
     *
     * Reach for {@link CommandFormProps.currentValues} to clear a property, or to supply one whose
     * value arrives late.
     */
    initialValues?: Partial<TCommand>;

    /**
     * The reactive overlay for values owned elsewhere - typically a query that answers after the
     * form has already rendered. Every change to it is written to the command.
     *
     * Unlike {@link CommandFormProps.initialValues} this layer carries presence semantics: a key it
     * holds is written whatever it holds, so both `null` and an explicitly present `undefined`
     * clear the property. Only a key that is absent altogether is left alone.
     */
    currentValues?: Partial<TCommand> | undefined;
    onFieldValidate?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown) => string | undefined;
    onFieldChange?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown, validationInfo?: FieldValidationInfo) => void;
    onBeforeExecute?: BeforeExecuteCallback<TCommand>;
    onSuccess?: (response: TResponse) => void;
    onFailed?: (commandResult: ICommandResult<TResponse>) => void;
    onException?: (messages: string[], stackTrace: string) => void;
    onUnauthorized?: () => void;
    onValidationFailure?: (validationResults: ValidationResult[]) => void;
    showTitles?: boolean;
    showErrors?: boolean;
    validateOn?: 'blur' | 'change' | 'both';
    validateAllFieldsOnChange?: boolean;
    validateOnInit?: boolean;
    /**
     * Whether silent/eager validation (on init, on change, on blur) should be performed against the
     * server. Defaults to false — silent validation only runs client-side rules and never contacts the
     * server. Executing the command still performs full server-side validation regardless of this flag;
     * enable this only to surface server-only rules (e.g. uniqueness checks) before submission.
     */
    autoServerValidate?: boolean;
    autoServerValidateThrottle?: number;
    fieldContainerComponent?: React.ComponentType<FieldContainerProps>;
    fieldDecoratorComponent?: React.ComponentType<FieldDecoratorProps>;
    errorDisplayComponent?: React.ComponentType<ErrorDisplayProps>;
    tooltipComponent?: React.ComponentType<TooltipWrapperProps>;
    errorClassName?: string;
    iconAddonClassName?: string;

    /**
     * Handle for driving the form from outside it.
     *
     * A named prop rather than a forwarded ref on purpose: `React.forwardRef` erases the generic
     * parameters, and consumers write `<CommandForm<TCommand, TResponse> …>` with explicit type
     * arguments, so forwarding would cost them their typing.
     */
    formRef?: React.Ref<CommandFormHandle>;

    /**
     * The form's content - fields, columns, and anything else to render between them.
     *
     * Pass a function to read the form's own execution state while rendering, which is what a submit
     * button outside the field list needs in order to disable itself while the command is in flight.
     */
    children?: React.ReactNode | ((state: CommandFormState) => React.ReactNode);
}

/**
 * The form state handed to a {@link CommandFormProps.children} render function.
 */
export interface CommandFormState {
    /**
     * Whether the command is currently executing.
     */
    isExecuting: boolean;
}

/**
 * Handle exposed through {@link CommandFormProps.formRef} for driving a form from outside it.
 */
export interface CommandFormHandle {
    /**
     * Executes the command the form is bound to, exactly as submitting the form does.
     * @returns The {@link ICommandResult} the command produced.
     */
    execute(): Promise<ICommandResult<unknown>>;

    /**
     * Whether the command is currently executing.
     */
    readonly isExecuting: boolean;
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

/**
 * Hook for reading whether the surrounding form's command is currently executing.
 *
 * A context built before the form reported this - a spec or a wrapper composing one by hand - reads as
 * not executing, which is what a caller disabling a button on it should assume when nobody is claiming
 * a command is in flight.
 * @returns True while the command is executing.
 */
export const useIsCommandExecuting = (): boolean => {
    const { isExecuting } = useCommandFormContext();
    return isExecuting ?? false;
};

const getCommandFormFields = <TCommand,>(props: { children?: React.ReactNode }): { fieldsOrColumns: React.ReactElement[] | ColumnInfo[], otherChildren: React.ReactNode[], initialValuesFromFields: Partial<TCommand>, orderedChildren: Array<{ type: 'field' | 'other', content: React.ReactNode, index: number }> } => {
    if (!props.children) {
        return { fieldsOrColumns: [], otherChildren: [], initialValuesFromFields: {}, orderedChildren: [] };
    }
    const fields: React.ReactElement<CommandFormFieldProps>[] = [];
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
        if (isCommandFormColumn(component)) {
            hasColumns = true;
            const childProps = child.props as { children?: React.ReactNode };
            const columnFields = React.Children.toArray(childProps.children).filter(child => {
                if (React.isValidElement(child)) {
                    const comp = child.type as React.ComponentType<unknown>;
                    if (isCommandFormField(comp)) {
                        extractInitialValue(child as React.ReactElement);
                        return true;
                    }
                }
                return false;
            }) as React.ReactElement[];
            columns.push({ fields: columnFields as React.ReactElement<CommandFormFieldProps>[] });
        }
        // Check if child is a CommandFormField (direct child)
        else if (isCommandFormField(component)) {
            extractInitialValue(child as React.ReactElement);
            fields.push(child as React.ReactElement<CommandFormFieldProps>);
            orderedChildren.push({ type: 'field', content: child, index: fieldIndex++ });
        }

        // Everything else is not a field, keep it as other children
        else {
            otherChildren.push(child);
            orderedChildren.push({ type: 'other', content: child, index: otherIndex++ });
        }
    });

    return { fieldsOrColumns: hasColumns ? columns : fields, otherChildren, initialValuesFromFields, orderedChildren };
};

const CommandFormComponent = <TCommand extends object = object, TResponse = object>(props: CommandFormProps<TCommand, TResponse>) => {
    const [isExecuting, setIsExecuting] = useState(false);

    // A render function has to become nodes before anything looks at the children: the field scan below
    // walks them with React.Children.toArray, and a function reaching that walk is rendered as a child,
    // which React refuses. Resolving here also makes the state the function reads a dependency of the
    // scan - keying it on props.children alone would leave the rendered output frozen at whatever
    // isExecuting was when the children last changed.
    const resolvedChildren = useMemo(
        () => typeof props.children === 'function' ? props.children({ isExecuting }) : props.children,
        [props.children, isExecuting]);

    const { fieldsOrColumns, initialValuesFromFields, orderedChildren } = useMemo(
        () => getCommandFormFields<TCommand>({ children: resolvedChildren }), [resolvedChildren]);

    // Extract matching properties from currentValues
    const valuesFromCurrentValues = useMemo(() => {
        if (!props.currentValues) return {};

        const tempCommand = new props.command() as Command;
        const commandProperties = tempCommand.propertyDescriptors.map(propertyDescriptor => propertyDescriptor.name);
        const extracted: Partial<TCommand> = {};

        commandProperties.forEach((propertyName: string) => {
            if (Object.prototype.hasOwnProperty.call(props.currentValues, propertyName)) {
                (extracted as Record<string, unknown>)[propertyName] = (props.currentValues as Record<string, unknown>)[propertyName];
            }
        });

        return extracted;
    }, [props.currentValues, props.command]);

    // Merge initialValues prop with values extracted from field currentValue props and currentValues.
    // The two seed layers skip undefined, the reactive one does not - see CommandFormProps. A field's
    // currentValue is skipped where it is extracted (nothing to seed), and props.initialValues here,
    // so neither of them spreads an undefined over a value currentValues has already resolved.
    const mergedInitialValues = useMemo(() => ({
        ...valuesFromCurrentValues,
        ...initialValuesFromFields,
        ...withoutUndefinedValues(props.initialValues)
    }), [valuesFromCurrentValues, initialValuesFromFields, props.initialValues]);

    // useCommand returns [instance, setter, clearer] for the typed command. Provide generics so commandInstance is TCommand.
    // Using type assertion through unknown to work around generic constraint mismatch
    const useCommandResult = useCommand(props.command as unknown as Constructor<Command<Partial<TCommand>, object>>, mergedInitialValues);
    const commandInstance = useCommandResult[0] as unknown as TCommand;
    const setCommandValuesInternal = useCommandResult[1] as SetCommandValues<TCommand>;
    const [commandVersion, setCommandVersion] = useState(0);
    const setCommandValues = useCallback((values: TCommand) => {
        setCommandValuesInternal(values);
        setCommandVersion(version => version + 1);
    }, [setCommandValuesInternal]);
    const [commandResult, setCommandResult] = useState<ICommandResult<unknown> | undefined>(undefined);
    const [silentValidationResult, setSilentValidationResult] = useState<ICommandResult<unknown> | undefined>(undefined);
    const [customFieldErrors, setCustomFieldErrors] = useState<Record<string, string>>({});
    const initializedRef = React.useRef(false);
    const lastServerValidateVersion = React.useRef<number>(-1);
    const serverValidateThrottleTimer = React.useRef<NodeJS.Timeout | null>(null);
    const silentValidationIssue = React.useRef(0);
    const lastAppliedSilentValidationIssue = React.useRef(-1);

    // Validation is asynchronous and several runs are legitimately in flight at once: the init effect
    // below, the per-keystroke run in CommandFormFields, and the throttled server round trip. Writing
    // whichever one resolves last makes the winner arrival order rather than issue order, so a slower
    // run describing values the form no longer holds lands after a faster run describing the current
    // ones and overwrites it. isValid is derived from this single slot and nothing recomputes it, so a
    // stale rejection greys out submit permanently - with every field valid and no message shown -
    // until some unrelated interaction happens to schedule another validation.
    //
    // Each run claims a token before it starts and hands it back with its result; a result is applied
    // only when no later one has been applied already. Ordering is restored without cancelling
    // anything, so a run that is still the newest always lands even when it is the slow one.
    const beginSilentValidation = useCallback(() => silentValidationIssue.current++, []);

    const applySilentValidationResult = useCallback((validationResult: ICommandResult<unknown>, issue?: number) => {
        if (issue === undefined) {
            // An unguarded write - a custom field going through the context - is taken as current, so
            // anything issued before it is stale from here on.
            lastAppliedSilentValidationIssue.current = silentValidationIssue.current;
        } else {
            if (issue < lastAppliedSilentValidationIssue.current) return false;
            lastAppliedSilentValidationIssue.current = issue;
        }
        setSilentValidationResult(validationResult);
        return true;
    }, []);

    const validateSilently = useCallback(async (showErrors: boolean) => {
        const issue = beginSilentValidation();
        const validationResult = await runCommandValidation(commandInstance, props.autoServerValidate ?? false);
        if (!validationResult) return;
        if (!applySilentValidationResult(validationResult, issue)) return;
        if (showErrors) {
            setCommandResult(validationResult);
        }
    }, [commandInstance, props.autoServerValidate, beginSilentValidation, applySilentValidationResult]);

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

        // Always run silent client validation on init and after currentValues changes to determine if the form is valid.
        // This ensures isValid reflects the real validity state from the first render,
        // even when no error messages are shown yet.
        // Error messages are only rendered (via commandResult) when validateOnInit is true.
        if (!initializedRef.current) {
            initializedRef.current = true;
            void validateSilently(props.validateOnInit ?? false);
        } else if (hasCurrentValues) {
            void validateSilently(props.validateOnInit ?? false);
        }
    }, [mergedInitialValues, props.validateOnInit, props.currentValues, setCommandValues, validateSilently]);

    // isValid is driven exclusively by silentValidationResult which is updated on mount and
    // after every field value change. commandResult only controls error message display.
    // Default to false (not yet validated) so the form is never considered valid before
    // the first silent validation completes.
    const isValid = silentValidationResult
        ? (silentValidationResult.validationResults?.length ?? 0) === 0
        : false;

    // isAuthorized checks if the current user has at least one of the roles required by the command.
    // If the command has no roles defined, all users are considered authorized.
    const identity = useIdentity();
    const commandRoles = (commandInstance as unknown as Command).roles ?? [];
    const isAuthorized = commandRoles.length === 0 || commandRoles.some(role => identity.isInRole(role));

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

        // Only call server validate if silent validation has run and all fields are valid
        const allFieldsValid = silentValidationResult !== undefined && isValid;
        
        // Check if we've already validated this command version
        const alreadyValidatedThisVersion = lastServerValidateVersion.current === commandVersion;
        
        // Must have all fields valid and not already validated this version
        if (allFieldsValid && !alreadyValidatedThisVersion && commandInstance && typeof (commandInstance as Record<string, unknown>).validate === 'function') {
            const performValidation = () => {
                lastServerValidateVersion.current = commandVersion;
                const issue = beginSilentValidation();
                void (async () => {
                    try {
                        const validationResult = await ((commandInstance as Record<string, unknown>).validate as () => Promise<ICommandResult<unknown>>)();
                        if (validationResult) {
                            // This is the only server round trip a typing burst makes, so its verdict has to
                            // reach isValid too. The per-keystroke validation in CommandFormFields is
                            // client-side only; without this, a rule the client cannot express - a uniqueness
                            // check, anything reading a read model - would leave the form reading as valid
                            // right up until submit failed. It is also the slowest run there is, so it is the
                            // one most likely to be overtaken - hence the token.
                            if (!applySilentValidationResult(validationResult, issue)) return;
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
    }, [props.autoServerValidate, props.autoServerValidateThrottle, commandInstance, commandVersion, isValid, silentValidationResult, beginSilentValidation, applySilentValidationResult]);

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
            if (memberMatchesField(validationResult.members, propertyName)) {
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
            // Cleared in a finally rather than after the callbacks: execute() rejects on a transport
            // failure, and a form that stayed executing forever would never let the caller try again.
            setIsExecuting(true);
            let result: ICommandResult<TResponse>;
            try {
                result = await (finalValues as unknown as Command).execute() as ICommandResult<TResponse>;
            } finally {
                setIsExecuting(false);
            }
            setCommandResult(result);

            // Invoke callbacks based on result state
            if (result.isSuccess && props.onSuccess) {
                props.onSuccess(result.response as TResponse);
            }
            if (!result.isSuccess && props.onFailed) {
                props.onFailed(result);
            }
            if (result.hasExceptions && props.onException) {
                props.onException(result.exceptionMessages, result.exceptionStackTrace);
            }
            if (!result.isAuthorized && props.onUnauthorized) {
                props.onUnauthorized();
            }
            if (!result.isValid && props.onValidationFailure) {
                props.onValidationFailure(result.validationResults);
            }

            return result;
        }

        throw new Error('Command instance does not have an execute method');
    }, [commandInstance, props, setCommandValues, setCommandResult]);

    const handleFormSubmit = useCallback((e: React.FormEvent) => {
        e.preventDefault();
        e.stopPropagation();
        void handleExecute();
    }, [handleExecute]);

    // Rebuilt whenever either member changes: the handle carries isExecuting as a value, so a handle
    // held across a render would otherwise keep reporting the state the form was in when it was made.
    useImperativeHandle(props.formRef, () => ({
        execute: handleExecute,
        isExecuting
    }), [handleExecute, isExecuting]);

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
        isAuthorized,
        isExecuting,
        beginSilentValidation,
        setSilentValidationResult: applySilentValidationResult,
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
                    if (isCommandFormField(component)) {
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

markAsCommandFormColumn(CommandFormColumnComponent);

// Export as function to enable proper type inference from command prop
export function CommandForm<TCommand extends object = object, TResponse = object>(
    props: CommandFormProps<TCommand, TResponse>
): React.ReactElement {
    return <CommandFormComponent<TCommand, TResponse> {...props} />;
}

// Attach static members
CommandForm.Column = CommandFormColumnComponent;
