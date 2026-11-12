// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useCallback, useState } from 'react';
import type { ICommandResult } from '@cratis/arc/commands';
import { runCommandValidation } from './runCommandValidation.js';

/**
 * Runs and orders the silent validation of a command form.
 *
 * Validation is asynchronous and several runs are legitimately in flight at once: the init effect,
 * the per-keystroke run in CommandFormFields, and the throttled server round trip. Writing
 * whichever one resolves last makes the winner arrival order rather than issue order, so a slower
 * run describing values the form no longer holds lands after a faster run describing the current
 * ones and overwrites it. isValid is derived from this single slot and nothing recomputes it, so a
 * stale rejection greys out submit permanently - with every field valid and no message shown -
 * until some unrelated interaction happens to schedule another validation.
 *
 * Each run claims a token before it starts and hands it back with its result; a result is applied
 * only when no later one has been applied already. Ordering is restored without cancelling
 * anything, so a run that is still the newest always lands even when it is the slow one.
 * @param {object} commandInstance The command to validate.
 * @param {boolean | undefined} autoServerValidate Whether the silent validation also contacts the server.
 * @param {Function} setCommandResult Publishes a validation result as the form's command result.
 * @returns The silent validation result and the functions to run and apply validations.
 */
export function useSilentValidation(
    commandInstance: object,
    autoServerValidate: boolean | undefined,
    setCommandResult: (result: ICommandResult<unknown>) => void,
) {
    const [silentValidationResult, setSilentValidationResult] = useState<
        ICommandResult<unknown> | undefined
    >(undefined);
    const silentValidationIssue = React.useRef(0);
    const lastAppliedSilentValidationIssue = React.useRef(-1);

    const beginSilentValidation = useCallback(() => silentValidationIssue.current++, []);

    const applySilentValidationResult = useCallback(
        (validationResult: ICommandResult<unknown>, issue?: number) => {
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
        },
        [],
    );

    const validateSilently = useCallback(
        async (showErrors: boolean) => {
            const issue = beginSilentValidation();
            const validationResult = await runCommandValidation(
                commandInstance,
                autoServerValidate ?? false,
            );
            if (!validationResult) return;
            if (!applySilentValidationResult(validationResult, issue)) return;
            if (showErrors) {
                setCommandResult(validationResult);
            }
        },
        [
            commandInstance,
            autoServerValidate,
            beginSilentValidation,
            applySilentValidationResult,
            setCommandResult,
        ],
    );

    return {
        silentValidationResult,
        beginSilentValidation,
        applySilentValidationResult,
        validateSilently,
    };
}
