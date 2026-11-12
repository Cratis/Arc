// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ICommandResult } from '@cratis/arc/commands';
import type { ValidationResultSeverity } from '@cratis/arc/validation';
import type { CommandFormContextValue, FieldValidationInfo } from './CommandFormContext.js';
import type { CommandFormFieldProps } from './CommandFormField.js';
import { blocksCommandValidation } from './blocksCommandValidation.js';
import { memberMatchesField } from './memberMatchesField.js';
import { runCommandValidation } from './runCommandValidation.js';

/**
 * Everything the value-change and blur handlers of a single command form field need to do their work.
 */
export interface CommandFormFieldHandlerScope {
    context: CommandFormContextValue<unknown>;
    propertyName: string;
    policy?: ValidationResultSeverity;
    nativeCommandResult?: ICommandResult<unknown>;
    fieldProps: CommandFormFieldProps;
    currentValue: unknown;
}

type ValidationResults = ICommandResult<unknown>['validationResults'] | undefined;

function resultsForField(results: ValidationResults, propertyName: string) {
    return results?.filter((vr) => memberMatchesField(vr.members, propertyName)) ?? [];
}

function buildValidationInfo(
    results: ValidationResults,
    propertyName: string,
    policy?: ValidationResultSeverity,
): FieldValidationInfo {
    return {
        isValid: !results?.some(
            (vr) => memberMatchesField(vr.members, propertyName) && blocksCommandValidation(vr, policy),
        ),
        errors: resultsForField(results, propertyName).map((vr) => vr.message),
    };
}

/**
 * Applies a validation result to the form: either as is, or merged per field so that the errors of
 * untouched fields are kept and only this field's errors are updated.
 */
function applyValidationResult(
    scope: CommandFormFieldHandlerScope,
    validationResult: ICommandResult<unknown>,
): void {
    const { context, propertyName, policy, nativeCommandResult } = scope;
    if (context.validateAllFieldsOnChange) {
        context.setCommandResult(validationResult);
        return;
    }

    const currentErrors = nativeCommandResult?.validationResults || [];
    const errorsFromOtherFields = currentErrors.filter(
        (vr) => !memberMatchesField(vr.members, propertyName),
    );
    const mergedValidationResults = [
        ...errorsFromOtherFields,
        ...resultsForField(validationResult.validationResults, propertyName),
    ];
    context.setCommandResult({
        ...validationResult,
        validationResults: mergedValidationResults,
        isValid:
            validationResult.isValid &&
            !mergedValidationResults.some((vr) => blocksCommandValidation(vr, policy)),
    });
}

function runCustomFieldValidation(scope: CommandFormFieldHandlerScope, value: unknown): void {
    const { context, propertyName, currentValue } = scope;
    if (!context.onFieldValidate) {
        return;
    }

    // Called SYNCHRONOUSLY — user-provided logic, no async. This runs before validate() so the
    // result is available immediately (no await).
    const validationError = context.onFieldValidate(
        context.commandInstance as Record<string, unknown>,
        propertyName,
        currentValue,
        value,
    );
    context.setCustomFieldError(propertyName, validationError);
}

function notifyFieldChanged(scope: CommandFormFieldHandlerScope, value: unknown): void {
    const { context, propertyName, policy, currentValue } = scope;
    if (!context.onFieldChange) {
        return;
    }

    // Called SYNCHRONOUSLY — fired on every value change. Uses previously computed errors for
    // immediate validationInfo; the full silent validation result updates asynchronously.
    context.onFieldChange(
        context.commandInstance as Record<string, unknown>,
        propertyName,
        currentValue,
        value,
        buildValidationInfo(context.commandResult?.validationResults, propertyName, policy),
    );
}

async function validateAfterChange(scope: CommandFormFieldHandlerScope): Promise<void> {
    const { context } = scope;

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
    const validationResult = await runCommandValidation(context.commandInstance, false);
    const describesCurrentValues =
        validationResult !== undefined &&
        context.setSilentValidationResult(validationResult, issue);

    // Show validation error messages based on the validateOn setting.
    // This is purely a display concern and does NOT affect isValid, but a message describing
    // values the field no longer holds is as wrong on screen as it is in isValid.
    const shouldValidateOnChange =
        context.validateOn === 'change' || context.validateOn === 'both';
    if (shouldValidateOnChange && describesCurrentValues && validationResult) {
        applyValidationResult(scope, validationResult);
    }
}

/**
 * Creates the handler invoked when the value of a command form field changes.
 * @param {CommandFormFieldHandlerScope} scope The scope of the field.
 * @returns {Function} The value change handler.
 */
export function createFieldValueChangeHandler(scope: CommandFormFieldHandlerScope) {
    const { context, propertyName, fieldProps } = scope;
    return async (value: unknown) => {
        if (propertyName) {
            // Update the command value (mutates command.current in-place immediately —
            // useCommand uses a useRef so validate() sees the new value right away).
            context.setCommandValues({ [propertyName]: value } as Record<string, unknown>);
            runCustomFieldValidation(scope, value);
            notifyFieldChanged(scope, value);
            await validateAfterChange(scope);
        }
        fieldProps.onValueChange?.(value);
        fieldProps.onChange?.(value);
    };
}

async function validateOnBlur(
    scope: CommandFormFieldHandlerScope,
): Promise<ICommandResult<unknown> | undefined> {
    const { context } = scope;

    // With autoServerValidate this run crosses the network, which makes it the one most
    // likely to be overtaken by a client-side run issued after it.
    const issue = context.beginSilentValidation();
    const validationResult = await runCommandValidation(
        context.commandInstance,
        context.autoServerValidate,
    );

    // Keep silent result current (covers edge cases where blur fires
    // without a preceding onChange, e.g. clipboard paste in some browsers).
    if (validationResult && context.setSilentValidationResult(validationResult, issue)) {
        applyValidationResult(scope, validationResult);
    }

    return validationResult;
}

function notifyFieldBlurred(
    scope: CommandFormFieldHandlerScope,
    validationResult: ICommandResult<unknown>,
): void {
    const { context, propertyName, policy } = scope;
    if (!context.onFieldChange) {
        return;
    }

    const currentValue = (context.commandInstance as Record<string, unknown>)[propertyName];
    context.onFieldChange(
        context.commandInstance as Record<string, unknown>,
        propertyName,
        currentValue,
        currentValue,
        buildValidationInfo(validationResult.validationResults, propertyName, policy),
    );
}

/**
 * Creates the handler invoked when a command form field loses focus.
 * @param {CommandFormFieldHandlerScope} scope The scope of the field.
 * @returns {Function} The blur handler.
 */
export function createFieldBlurHandler(scope: CommandFormFieldHandlerScope) {
    const { context, propertyName } = scope;
    return async () => {
        if (!propertyName) {
            return;
        }

        const shouldValidateOnBlur = context.validateOn === 'blur' || context.validateOn === 'both';
        const validationResult = shouldValidateOnBlur ? await validateOnBlur(scope) : undefined;

        if (validationResult) {
            notifyFieldBlurred(scope, validationResult);
        }
    };
}
