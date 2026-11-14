// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { ICommandResult } from '@cratis/arc/commands';
import {
    classifyValidationOutcome,
    describeDisabledServer,
    initialValidationState,
    invalidateFreshness,
    sameValidationState,
    type CommandFormValidationChannel,
    type CommandFormValidationChannelState,
    type CommandFormValidationOutcome,
    type CommandFormValidationState,
} from './commandFormValidationState.js';

interface IssuedValidation {
    channel: CommandFormValidationChannel;
    revision: number;
}

/**
 * Tracks what the validation of a command form has concluded, per channel, and how fresh it is.
 *
 * It rides on the issue tokens of {@link useSilentValidation}: a validation is registered when its
 * token is claimed - against the revision of the values it will read - and settled when that token
 * is applied or abandoned. A verdict is `current` only when it describes the revision the form holds
 * now and no later validation was issued on its channel; otherwise it is `stale`. Nothing here is
 * inferred from a promise resolving: whether a result came from the server is decided from the result.
 * @param {object} commandInstance The command whose validation is tracked.
 * @param {boolean | undefined} autoServerValidate Whether the form validates against the server.
 * @returns The published state and the functions that feed it.
 */
export function useCommandFormValidationState(
    commandInstance: object,
    autoServerValidate: boolean | undefined,
) {
    const revision = React.useRef(0);
    const issued = React.useRef(new Map<number, IssuedValidation>());
    const latestIssue = React.useRef<Record<CommandFormValidationChannel, number>>({ client: -1, server: -1 });
    const scheduled = React.useRef(false);
    const settled = React.useRef(initialValidationState());
    const mounted = React.useRef(true);
    const commandRef = React.useRef(commandInstance);
    commandRef.current = commandInstance;
    const [published, setPublished] = useState(initialValidationState);

    useEffect(() => {
        mounted.current = true;
        return () => {
            mounted.current = false;
        };
    }, []);

    const publish = useCallback(() => {
        if (!mounted.current) return;
        const pending = (channel: CommandFormValidationChannel) =>
            [...issued.current.values()].some((entry) => entry.channel === channel) ||
            (channel === 'server' && scheduled.current);
        const next: CommandFormValidationState = {
            client: { ...settled.current.client, pending: pending('client') },
            server: { ...settled.current.server, pending: pending('server') },
        };
        setPublished((previous) => (sameValidationState(previous, next) ? previous : next));
    }, []);

    const invalidate = useCallback(() => {
        revision.current += 1;
        settled.current = invalidateFreshness(settled.current);
        publish();
    }, [publish]);

    const schedule = useCallback((isScheduled: boolean) => {
        scheduled.current = isScheduled;
        publish();
    }, [publish]);

    const begin = useCallback((issue: number, channel: CommandFormValidationChannel) => {
        issued.current.set(issue, { channel, revision: revision.current });
        latestIssue.current[channel] = issue;
        publish();
    }, [publish]);

    // A result counts as the server's only when the server could have produced it: Command.validate
    // answers locally, without a request, when the client rules reject, and that answer is the
    // client's verdict whatever channel the caller asked for.
    const attribute = useCallback((
        hint: CommandFormValidationChannel,
        result: ICommandResult<unknown>,
    ): CommandFormValidationChannel => {
        if (hint === 'client') return 'client';
        const command = commandRef.current as Record<string, unknown>;
        if (typeof command.validate !== 'function') return 'client';
        if (result.isValid || typeof command.validateClientSide !== 'function') return 'server';
        const clientSide = (command.validateClientSide as () => ICommandResult<unknown>)();
        return clientSide.isValid ? 'server' : 'client';
    }, []);

    const record = useCallback((
        entry: IssuedValidation,
        issue: number,
        channel: CommandFormValidationChannel,
        outcome: CommandFormValidationOutcome,
    ) => {
        const current: CommandFormValidationChannelState = outcome.notRunReason
            ? { pending: false, freshness: 'unvalidated', verdict: 'unknown', notRunReason: outcome.notRunReason }
            : {
                pending: false,
                verdict: outcome.verdict,
                validatedVersion: entry.revision,
                freshness: entry.revision === revision.current && latestIssue.current[entry.channel] === issue
                    ? 'current'
                    : 'stale',
            };
        settled.current = { ...settled.current, [channel]: current };
    }, []);

    // Called once the issue has been through the ordering of useSilentValidation. A result that was
    // overtaken is discarded: it has nothing to say about the values the form holds now.
    const settle = useCallback((issue: number, result: ICommandResult<unknown>, applied: boolean) => {
        const entry = issued.current.get(issue);
        if (!entry) return;
        issued.current.delete(issue);
        if (applied) {
            record(entry, issue, attribute(entry.channel, result), classifyValidationOutcome(result));
        }
        publish();
    }, [publish, attribute, record]);

    // The attempt threw. It only matters while it is the newest on its channel; an older one that
    // fails after a newer one was issued describes nothing anyone is waiting for.
    const fail = useCallback((issue: number) => {
        const entry = issued.current.get(issue);
        if (!entry) return;
        issued.current.delete(issue);
        if (latestIssue.current[entry.channel] === issue) {
            record(entry, issue, entry.channel, classifyValidationOutcome(undefined));
        }
        publish();
    }, [publish, record]);

    const validation = useMemo(
        () => describeDisabledServer(published, autoServerValidate ?? false),
        [published, autoServerValidate],
    );

    const tracker = useMemo(() => ({ begin, settle, fail }), [begin, settle, fail]);

    return { validation, invalidate, schedule, tracker };
}
