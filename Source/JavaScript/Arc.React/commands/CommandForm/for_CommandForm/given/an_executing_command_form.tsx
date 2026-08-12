// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, RenderResult } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { CommandForm, CommandFormHandle, useIsCommandExecuting } from '../../CommandForm';
import { TestCommand } from '../TestCommand';
import { RejectingCommand } from './a_rejecting_command';
import { ArcContext } from '../../../../ArcContext';

/**
 * Reports what the surrounding form's context says, so a spec can tell the context apart from the
 * render prop rather than trusting that publishing one published the other.
 */
const ExecutionProbe = ({ onRead }: { onRead: (isExecuting: boolean) => void }) => {
    onRead(useIsCommandExecuting());
    return null;
};

/**
 * Renders a {@link CommandForm} whose command reaches the network and is then held there, so a spec can
 * observe the form while the command is in flight rather than only after it has settled.
 *
 * TestCommand declares every property optional, so execute() clears client validation and issues a
 * request - which is what makes a pending window exist to observe at all.
 */
export class an_executing_command_form {
    fetchHelper: ReturnType<typeof createFetchHelper>;
    fetchStub!: sinon.SinonStub;
    formRef = React.createRef<CommandFormHandle>();
    renderResult!: RenderResult;

    /** Every value the render prop was given, in order. */
    renderPropReadings: boolean[] = [];

    /** Every value the form context reported, in order. */
    contextReadings: boolean[] = [];

    private execution?: Promise<unknown>;
    private settle!: (response: Response) => void;
    private fail!: (reason: Error) => void;

    constructor() {
        this.fetchHelper = createFetchHelper();
    }

    renderForm() {
        // Per spec, not per suite - given() builds this class once per describe, and cleanup() runs
        // after every it, so a stub installed in the constructor would be gone from the second one on.
        this.fetchStub = this.fetchHelper.stubFetch();
        this.fetchStub.returns(new Promise<Response>((resolve, reject) => {
            this.settle = resolve;
            this.fail = reject;
        }));

        this.renderResult = render(
            <ArcContext.Provider value={{ microservice: 'test-microservice', apiBasePath: '/api', origin: 'https://example.com' }}>
                <CommandForm<TestCommand> command={TestCommand} formRef={this.formRef}>
                    {({ isExecuting }) => {
                        this.renderPropReadings.push(isExecuting);
                        return <ExecutionProbe onRead={reading => this.contextReadings.push(reading)} />;
                    }}
                </CommandForm>
            </ArcContext.Provider>
        );
    }

    /**
     * Renders a form bound to a command whose execution rejects outright.
     *
     * Nothing is held open here - the rejection is immediate, and what the spec is looking at is what
     * the form is left reporting once it has happened.
     */
    renderFormWithRejectingCommand() {
        this.fetchStub = this.fetchHelper.stubFetch();

        this.renderResult = render(
            <ArcContext.Provider value={{ microservice: 'test-microservice', apiBasePath: '/api', origin: 'https://example.com' }}>
                <CommandForm<RejectingCommand> command={RejectingCommand} formRef={this.formRef}>
                    {({ isExecuting }) => {
                        this.renderPropReadings.push(isExecuting);
                        return null;
                    }}
                </CommandForm>
            </ArcContext.Provider>
        );
    }

    /**
     * Executes and waits for the execution to settle however it settles.
     */
    async executeAndSettle() {
        await act(async () => {
            await this.formRef.current!.execute().catch(() => { /* the rejection is the scenario */ });
            await this.drainPromises();
        });
    }

    /**
     * Starts the command and leaves it in flight. The request is held open, so what this returns to is
     * the middle of an execution, not the end of one.
     */
    async beginExecute() {
        await act(async () => {
            this.execution = this.formRef.current!.execute().catch(() => { /* settled by the spec */ });
            await this.drainPromises();
        });
    }

    /**
     * Answers the held request successfully and lets the execution finish.
     */
    async completeExecute() {
        this.settle({ ok: true, status: 200, json: async () => ({ isSuccess: true }) } as Response);
        await this.finishExecution();
    }

    /**
     * Fails the held request outright, the way a transport error does.
     */
    async failExecute() {
        this.fail(new Error('Failed to fetch'));
        await this.finishExecution();
    }

    /** What the handle reports right now. */
    get isExecutingOnHandle() {
        return this.formRef.current!.isExecuting;
    }

    /** The most recent value the render prop was given. */
    get lastRenderPropReading() {
        return this.renderPropReadings[this.renderPropReadings.length - 1];
    }

    /** The most recent value the form context reported. */
    get lastContextReading() {
        return this.contextReadings[this.contextReadings.length - 1];
    }

    cleanup() {
        this.fetchHelper.restore();
        if (this.renderResult) {
            this.renderResult.unmount();
        }
    }

    private async finishExecution() {
        await act(async () => {
            await this.execution;
            await this.drainPromises();
        });
    }

    private drainPromises() {
        return new Promise(resolve => setTimeout(resolve, 0));
    }
}
