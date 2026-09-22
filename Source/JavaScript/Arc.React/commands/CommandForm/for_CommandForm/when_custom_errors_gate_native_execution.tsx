// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, fireEvent, render } from '@testing-library/react';
import { expect } from 'chai';
import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { Command, CommandResult, type ICommandResult } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { CommandForm, type CommandFormProps } from '../CommandForm';
import { useCommandFormContext, type CommandFormContextValue, type CommandFormHandle } from '../CommandFormContext';
import { InputTextField } from '../fields/InputTextField';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { a_command_form_context } from './given/a_command_form_context';

class ExampleCommand extends Command<{ code?: string }> {
    readonly route = '/examples/code';
    readonly propertyDescriptors = [new PropertyDescriptor('code', String, true)];
    code?: string;
    get requestParameters(): string[] { return []; }
    constructor() { super(Object, false); }
}

class ExampleRestrictedCommand extends ExampleCommand {
    readonly roles = ['example-editor'];
}

class ExampleRequiredCommand extends ExampleCommand {
    readonly propertyDescriptors = [new PropertyDescriptor('code', String, false)];
}

const responseBody = () => ({
    correlationId: Guid.empty.toString(),
    isSuccess: true,
    isAuthorized: true,
    isValid: true,
    hasExceptions: false,
    validationResults: [] as Array<{ severity: number; message: string; members: string[]; state: object }>,
    exceptionMessages: [] as string[],
    exceptionStackTrace: '',
    authorizationFailureReason: '',
    response: { accepted: true },
});
const httpResponse = (body = responseBody()) => new Response(JSON.stringify(body), {
    status: 200, headers: { 'Content-Type': 'application/json' },
});
const pendingResponse = () => {
    let resolve!: (response: Response) => void;
    const promise = new Promise<Response>((complete) => { resolve = complete; });
    return { promise, resolve };
};

// A synthetic formatted field keeps malformed text out of the typed command value. Neither a
// previously accepted value nor an optional undefined value makes that malformed text submittable.
const ExampleCodeField = () => {
    const context = useCommandFormContext<ExampleCommand>();
    const [text, setText] = React.useState('');
    return <input aria-label='Example code' value={text} onChange={(event) => {
        const value = event.target.value;
        setText(value);
        const valid = /^[A-Z]{3}$/.test(value);
        context.setCustomFieldError('code', valid ? undefined : 'Use three uppercase letters');
        if (valid) context.setCommandValues({ code: value } as ExampleCommand);
    }} />;
};

const ExampleFeedbackField = asCommandFormField(
    ({ invalid }: WrappedFieldProps) => <output data-testid='adapter-invalid'>{String(invalid)}</output>,
    { defaultValue: '' },
);

describe('when custom errors gate native execution', () => {
    let context: CommandFormContextValue<ExampleCommand>;
    let handle: React.RefObject<CommandFormHandle>;
    let fetchStub: sinon.SinonStub;
    let beforeExecute: sinon.SinonSpy;
    let success: sinon.SinonSpy;
    let failed: sinon.SinonSpy;
    let validationFailure: sinon.SinonSpy;
    let unauthorized: sinon.SinonSpy;
    let exception: sinon.SinonSpy;
    let stateChange: sinon.SinonSpy;

    const Probe = () => {
        context = useCommandFormContext<ExampleCommand>();
        return <>
            <output data-testid='validity'>{String(context.isValid)}</output>
            <output data-testid='result-summary'>{context.commandResult
                ? context.commandResult.isSuccess ? 'Accepted' : context.commandResult.validationResults.map((error) => error.message).join('; ')
                : 'No result'}</output>
        </>;
    };
    const mount = async (props: Partial<CommandFormProps<ExampleCommand>> = {}, child: React.ReactNode = <ExampleCodeField />) => {
        const result = render(
            <CommandForm command={ExampleCommand} formRef={handle}
                onBeforeExecute={beforeExecute} onSuccess={success} onFailed={failed}
                onValidationFailure={validationFailure} onUnauthorized={unauthorized}
                onException={exception} onStateChange={stateChange} {...props}>
                <Probe />
                {child}
                <button type='submit'>Submit example</button>
            </CommandForm>,
            { wrapper: new a_command_form_context().createWrapper() },
        );
        await act(() => Promise.resolve());
        return result;
    };
    const expectLocalFailure = (result: ICommandResult<unknown>, members = ['code']) => {
        expect(result).to.be.instanceOf(CommandResult);
        expect(result.isSuccess).to.equal(false);
        expect(result.isValid).to.equal(false);
        expect(result.isAuthorized).to.equal(true); // Native local-validation convention, not an auth verdict.
        expect(result.hasExceptions).to.equal(false);
        expect(result.correlationId).to.be.instanceOf(Guid);
        expect(result.exceptionMessages).to.deep.equal([]);
        expect(result.exceptionStackTrace).to.equal('');
        expect(result.authorizationFailureReason).to.equal('');
        expect(result.response).to.equal(undefined);
        expect(result.validationResults.flatMap((error) => error.members)).to.deep.equal(members);
        for (const error of result.validationResults) {
            expect(error).to.be.instanceOf(ValidationResult);
            expect(error.severity).to.equal(ValidationResultSeverity.Error);
        }
        expect(failed.lastCall.args[0]).to.equal(result);
        expect(validationFailure.lastCall.args[0]).to.equal(result.validationResults);
        expect(success.called).to.equal(false);
        expect(unauthorized.called).to.equal(false);
        expect(exception.called).to.equal(false);
    };

    beforeEach(() => {
        handle = React.createRef<CommandFormHandle>();
        fetchStub = sinon.stub(globalThis, 'fetch').callsFake(async () => httpResponse());
        beforeExecute = sinon.spy((command: ExampleCommand) => command);
        success = sinon.spy();
        failed = sinon.spy();
        validationFailure = sinon.spy();
        unauthorized = sinon.spy();
        exception = sinon.spy();
        stateChange = sinon.spy();
    });
    afterEach(() => { fetchStub.restore(); });

    it('blocks native submit before HTTP, transformation and success, and reports field members', async () => {
        const view = await mount({ onFieldValidate: () => 'Example is invalid' },
            <InputTextField<ExampleCommand> value={(command) => command.code} />);
        fireEvent.change(view.container.querySelector('input')!, { target: { value: 'invalid' } });
        fireEvent.submit(view.container.querySelector('form')!);
        await act(() => Promise.resolve());
        expect(fetchStub.callCount).to.equal(0);
        expect(beforeExecute.callCount).to.equal(0);
        expect(failed.callCount).to.equal(1);
        expect(validationFailure.callCount).to.equal(1);
        expectLocalFailure(failed.firstCall.args[0]);
        expect(view.getByText('Example is invalid', { selector: 'small' }).textContent).to.equal('Example is invalid');
        expect(context.isValid).to.equal(false);
        expect(handle.current!.isValid).to.equal(false);
        expect(view.getByTestId('validity').textContent).to.equal('false');
        expect(stateChange.lastCall.args[0].isValid).to.equal(false);
        expect(handle.current!.isExecuting).to.equal(false);
        expect(stateChange.args.every(([state]) => !state.isExecuting)).to.equal(true);
    });

    for (const surface of ['context', 'ref'] as const) {
        it(`blocks a captured ${surface} execution immediately after setting errors in the same event`, async () => {
            await mount();
            const capturedContext = context;
            const capturedHandle = handle.current!;
            const execute = surface === 'context' ? context.onExecute! : capturedHandle.execute;
            let result!: ICommandResult<unknown>;
            await act(async () => {
                capturedContext.setCustomFieldError('code', 'Example format error');
                capturedContext.setCustomFieldError('other', 'Another example error');
                expect(capturedContext.getFieldError('code')).to.equal('Example format error');
                expect(capturedContext.isValid).to.equal(false);
                expect(capturedHandle.isValid).to.equal(false);
                result = await execute();
            });
            expectLocalFailure(result, ['code', 'other']);
            expect(fetchStub.callCount).to.equal(0);
            expect(beforeExecute.callCount).to.equal(0);
        });
    }

    for (const fieldName of ['__proto__', 'constructor']) {
        for (const surface of ['submit', 'context', 'ref'] as const) {
            it(`preserves and clears an own ${fieldName} error through same-event ${surface} execution`, async () => {
                const view = await mount({},
                    <ExampleFeedbackField<ExampleCommand> fieldName={fieldName} value={(command) => command.code} />);
                expect(view.getByTestId('adapter-invalid').textContent).to.equal('false');
                const captured = context;
                const execute = surface === 'ref' ? handle.current!.execute : captured.onExecute!;
                expect(captured.getFieldError(fieldName)).to.equal(undefined);
                let result!: ICommandResult<unknown>;
                await act(async () => {
                    captured.setCustomFieldError(fieldName, 'Example reserved-name error');
                    expect(captured.getFieldError(fieldName)).to.equal('Example reserved-name error');
                    expect(handle.current!.isValid).to.equal(false);
                    if (surface === 'submit') {
                        fireEvent.submit(view.container.querySelector('form')!);
                        result = failed.lastCall.args[0];
                    } else {
                        result = await execute();
                    }
                });
                expectLocalFailure(result, [fieldName]);
                expect(view.getByTestId('adapter-invalid').textContent).to.equal('true');
                expect(result.validationResults[0].message).to.equal('Example reserved-name error');
                expect(Object.hasOwn(context.customFieldErrors, fieldName)).to.equal(true);
                expect(context.commandResult).to.equal(result);
                expect(fetchStub.callCount).to.equal(0);
                expect(beforeExecute.callCount).to.equal(0);
                expect(view.getByTestId('result-summary').textContent).to.equal('Example reserved-name error');
                await act(async () => {
                    captured.setCustomFieldError(fieldName, undefined);
                    expect(captured.getFieldError(fieldName)).to.equal(undefined);
                    expect(handle.current!.isValid).to.equal(true);
                    if (surface === 'submit') {
                        fireEvent.submit(view.container.querySelector('form')!);
                    } else {
                        expect((await execute()).isSuccess).to.equal(true);
                    }
                });
                expect(view.getByTestId('adapter-invalid').textContent).to.equal('false');
                expect(Object.hasOwn(context.customFieldErrors, fieldName)).to.equal(false);
                expect(context.getFieldError(fieldName)).to.equal(undefined);
                expect(fetchStub.callCount).to.equal(1);
                expect(success.callCount).to.equal(1);
                expect(context.commandResult!.isSuccess).to.equal(true);
                expect(view.getByTestId('result-summary').textContent).to.equal('Accepted');
            });
        }
    }

    for (const surface of ['submit', 'context', 'ref'] as const) {
        it(`publishes a blocked ${surface} result after native success and restores native feedback on clear before retry`, async () => {
            const view = await mount();
            let accepted!: ICommandResult<unknown>;
            await act(async () => { accepted = await handle.current!.execute(); });
            expect(context.commandResult).to.equal(accepted);
            expect(accepted.isSuccess).to.equal(true);
            expect(success.callCount).to.equal(1);
            expect(view.getByTestId('result-summary').textContent).to.equal('Accepted');
            success.resetHistory();
            const execute = surface === 'ref' ? handle.current!.execute : context.onExecute!;
            let blocked!: ICommandResult<unknown>;
            await act(async () => {
                context.setCustomFieldError('code', 'Example format error');
                if (surface === 'submit') {
                    fireEvent.submit(view.container.querySelector('form')!);
                    blocked = failed.lastCall.args[0];
                } else {
                    blocked = await execute();
                }
            });
            expectLocalFailure(blocked);
            expect(context.commandResult).to.equal(blocked);
            expect(view.getByTestId('result-summary').textContent).to.equal('Example format error');
            expect(fetchStub.callCount).to.equal(1);
            expect(beforeExecute.callCount).to.equal(1);
            await act(async () => { context.setCustomFieldError('code', undefined); });
            expect(context.commandResult).to.equal(accepted);
            expect(context.isValid).to.equal(true);
            expect(context.getFieldError('code')).to.equal(undefined);
            expect(view.getByTestId('result-summary').textContent).to.equal('Accepted');
            expect(success.callCount).to.equal(0); // Clearing is not a new execution or callback.
            let retried!: ICommandResult<unknown>;
            await act(async () => { retried = await execute(); });
            expect(context.commandResult).to.equal(retried);
            expect(retried.isSuccess).to.equal(true);
            expect(view.getByTestId('result-summary').textContent).to.equal('Accepted');
            expect(success.callCount).to.equal(1);
            expect(success.firstCall.args[0]).to.equal(retried.response);
            expect(fetchStub.callCount).to.equal(2);
            expect(failed.callCount).to.equal(1);
            expect(validationFailure.callCount).to.equal(1);
        });
    }

    it('updates only the custom failure while retaining native field errors and silent validity', async () => {
        const body = responseBody();
        body.isSuccess = false;
        body.isValid = false;
        body.validationResults = [{ severity: 2, message: 'Example server rule', members: ['code'], state: {} }];
        fetchStub.onFirstCall().resolves(httpResponse(body));
        const view = await mount();
        let native!: ICommandResult<unknown>;
        await act(async () => { native = await handle.current!.execute(); });
        expect(context.commandResult).to.equal(native);
        await act(async () => {
            context.setSilentValidationResult(native);
            context.setCustomFieldError('code', 'Example format error');
            context.setCustomFieldError('other', 'Another example error');
            await handle.current!.execute();
        });
        expect(context.commandResult).to.equal(failed.lastCall.args[0]);
        expect(view.getByTestId('result-summary').textContent).to.equal('Example format error; Another example error');
        await act(async () => {
            context.setCustomFieldError('code', undefined);
            expect(context.getFieldError('code')).to.equal('Example server rule');
        });
        expect(context.commandResult!.validationResults.map((error) => error.members)).to.deep.equal([['other']]);
        expect(view.getByTestId('result-summary').textContent).to.equal('Another example error');
        await act(async () => { context.setCustomFieldError('other', 'Updated example error'); });
        expect(view.getByTestId('result-summary').textContent).to.equal('Updated example error');
        await act(async () => { context.setCustomFieldError('other', ''); });
        expect(context.commandResult).to.equal(native);
        expect(context.getFieldError('code')).to.equal('Example server rule');
        expect(context.getFieldError('other')).to.equal(undefined);
        expect(context.isValid).to.equal(false);
        expect(handle.current!.isValid).to.equal(false);
        expect(view.getByTestId('result-summary').textContent).to.equal('Example server rule');
        expect(fetchStub.callCount).to.equal(1);
        expect(failed.callCount).to.equal(2);
        expect(validationFailure.callCount).to.equal(2);
    });

    for (const interaction of ['change', 'blur'] as const) {
        for (const hasNativeError of [false, true]) {
            it(`keeps custom failures out of ${interaction} feedback merges ${hasNativeError ? 'with' : 'without'} prior native errors`, async () => {
                const view = await mount({ validateOn: interaction },
                    <InputTextField<ExampleCommand> value={(command) => command.code} />);
                if (hasNativeError) {
                    const body = responseBody();
                    body.isSuccess = false;
                    body.isValid = false;
                    body.validationResults = [{ severity: 2, message: 'Native other error', members: ['other'], state: {} }];
                    fetchStub.onFirstCall().resolves(httpResponse(body));
                    await act(async () => { await handle.current!.execute(); });
                }
                await act(async () => {
                    context.setCustomFieldError('other', 'Custom other error');
                    await handle.current!.execute();
                });
                expect(view.getByTestId('result-summary').textContent).to.equal('Custom other error');
                const input = view.container.querySelector('input')!;
                if (interaction === 'change') fireEvent.change(input, { target: { value: 'ABC' } });
                else fireEvent.blur(input);
                await act(() => Promise.resolve());
                await act(async () => { context.setCustomFieldError('other', undefined); });
                expect(context.commandResult!.validationResults.map((error) => error.message))
                    .to.deep.equal(hasNativeError ? ['Native other error'] : []);
                expect(context.getFieldError('other')).to.equal(hasNativeError ? 'Native other error' : undefined);
                expect(context.isValid).to.equal(true);
                expect(handle.current!.isValid).to.equal(true);
                expect(fetchStub.callCount).to.equal(hasNativeError ? 1 : 0);
            });
        }
    }

    it('blocks an immediate execution from onFieldChange and permits the next valid field change', async () => {
        let execution!: Promise<ICommandResult<unknown>>;
        const view = await mount({
            onFieldValidate: (_command, _field, _old, value) => value === 'ABC' ? undefined : 'Example format error',
            onFieldChange: () => { execution = execute(); },
            validateOn: 'change',
        }, <InputTextField<ExampleCommand> value={(command) => command.code} />);
        const execute = context.onExecute!;
        fireEvent.change(view.container.querySelector('input')!, { target: { value: 'invalid' } });
        await act(async () => { expectLocalFailure(await execution); });
        expect(fetchStub.callCount).to.equal(0);
        expect(beforeExecute.callCount).to.equal(0);
        fireEvent.change(view.container.querySelector('input')!, { target: { value: 'ABC' } });
        await act(async () => { expect((await execution).isSuccess).to.equal(true); });
        expect(fetchStub.callCount).to.equal(1);
        expect(JSON.parse(fetchStub.firstCall.args[1].body)).to.deep.equal({ code: 'ABC' });
        expect(success.firstCall.args[0]).to.deep.equal({ accepted: true });
        expect(context.getFieldError('code')).to.equal(undefined);
        expect(context.isValid).to.equal(true);
    });

    it('retains another field error when only one of several errors clears', async () => {
        await mount();
        await act(async () => {
            context.setCustomFieldError('code', 'Example format error');
            context.setCustomFieldError('other', 'Another example error');
            context.setCustomFieldError('code', undefined);
            expectLocalFailure(await handle.current!.execute(), ['other']);
        });
        expect(fetchStub.callCount).to.equal(0);
        expect(context.getFieldError('code')).to.equal(undefined);
        expect(context.isValid).to.equal(false);
    });

    for (const previousValue of [undefined, 'ABC']) {
        it(`does not publish ${previousValue === undefined ? 'an optional undefined value' : 'a previous valid value'} when formatted input is invalid`, async () => {
            const view = await mount();
            const input = view.getByLabelText('Example code');
            if (previousValue) fireEvent.change(input, { target: { value: previousValue } });
            fireEvent.change(input, { target: { value: 'malformed' } });
            expect(context.commandInstance.code).to.equal(previousValue);
            fireEvent.submit(view.container.querySelector('form')!);
            await act(() => Promise.resolve());
            expect(fetchStub.callCount).to.equal(0);
            expectLocalFailure(failed.firstCall.args[0]);
        });
    }

    it('clears errors without sticky validation, permits valid HTTP and blocks repopulated errors after reset', async () => {
        const view = await mount({ validateOn: 'change' },
            <InputTextField<ExampleCommand> value={(command) => command.code} />);
        await act(async () => {
            context.setCustomFieldError('code', 'Example format error');
            await handle.current!.execute();
        });
        fireEvent.change(view.container.querySelector('input')!, { target: { value: 'XYZ' } });
        await act(() => Promise.resolve());
        const capturedExecute = context.onExecute!;
        await act(async () => {
            context.setCustomFieldError('code', undefined);
            expect(handle.current!.isValid).to.equal(true);
            expect(context.getFieldError('code')).to.equal(undefined);
            expect((await capturedExecute()).isSuccess).to.equal(true);
        });
        expect(fetchStub.callCount).to.equal(1);
        expect(JSON.parse(fetchStub.firstCall.args[1].body)).to.deep.equal({ code: 'XYZ' });
        expect(String(fetchStub.firstCall.args[0])).to.equal('https://example.com/api/examples/code');
        expect(beforeExecute.callCount).to.equal(1);
        expect(success.callCount).to.equal(1);
        expect(context.isValid).to.equal(true);
        expect(stateChange.lastCall.args[0].isValid).to.equal(true);
        await act(async () => {
            context.commandInstance.clear();
            context.setCustomFieldError('code', 'Repopulated example error');
            expect((await capturedExecute()).isValid).to.equal(false);
        });
        expect(fetchStub.callCount).to.equal(1);
        expect(handle.current!.isValid).to.equal(false);
    });

    it('treats an empty message as cleared, but a nonempty whitespace message as an error', async () => {
        await mount();
        await act(async () => {
            context.setCustomFieldError('code', ' ');
            expect((await context.onExecute!()).isValid).to.equal(false);
            context.setCustomFieldError('code', '');
            expect(context.getFieldError('code')).to.equal(undefined);
            expect(handle.current!.isValid).to.equal(true);
            expect((await handle.current!.execute()).isSuccess).to.equal(true);
        });
        expect(fetchStub.callCount).to.equal(1);
        expect(context.customFieldErrors).to.deep.equal({});
    });

    it('rechecks errors set by onBeforeExecute before calling the native executor', async () => {
        await mount({ onBeforeExecute: (command) => {
            context.setCustomFieldError('code', 'Rejected by example transformation');
            return command;
        } });
        let result!: ICommandResult<unknown>;
        await act(async () => { result = await handle.current!.execute(); });
        expectLocalFailure(result);
        expect(fetchStub.callCount).to.equal(0);
        expect(handle.current!.isExecuting).to.equal(false);
    });

    it('does not retry when a failure callback clears the error', async () => {
        await mount({ onFailed: (result) => {
            failed(result);
            context.setCustomFieldError('code', undefined);
        } });
        await act(async () => {
            context.setCustomFieldError('code', 'Example error');
            expect((await handle.current!.execute()).isValid).to.equal(false);
        });
        expect(fetchStub.callCount).to.equal(0);
        expect(handle.current!.isValid).to.equal(true);
        expect(validationFailure.callCount).to.equal(1);
    });

    it('keeps a custom error authoritative while native server validation is pending', async () => {
        const pending = pendingResponse();
        fetchStub.onFirstCall().returns(pending.promise);
        await mount({ autoServerValidate: true, autoServerValidateThrottle: 60000 });
        expect(fetchStub.callCount).to.equal(1);
        expect(String(fetchStub.firstCall.args[0])).to.equal('https://example.com/api/examples/code/validate');
        const execute = context.onExecute!;
        await act(async () => {
            context.setCustomFieldError('code', 'Pending example error');
            expect((await execute()).isValid).to.equal(false);
            pending.resolve(httpResponse());
        });
        expect(fetchStub.callCount).to.equal(1);
        expect(context.isValid).to.equal(false);
        expect(handle.current!.isValid).to.equal(false);
        expect(context.getFieldError('code')).to.equal('Pending example error');
        await act(async () => {
            context.setCustomFieldError('code', undefined);
            expect((await execute()).isSuccess).to.equal(true);
        });
        expect(fetchStub.callCount).to.equal(2);
    });

    it('does not revive stale native validation after a newer field change and custom error', async () => {
        const pending = pendingResponse();
        fetchStub.onFirstCall().returns(pending.promise);
        const view = await mount({ autoServerValidate: true, autoServerValidateThrottle: 60000 },
            <InputTextField<ExampleCommand> value={(command) => command.code} />);
        fireEvent.change(view.container.querySelector('input')!, { target: { value: 'NEW' } });
        await act(() => Promise.resolve());
        await act(async () => {
            context.setCustomFieldError('code', 'Current example error');
            const body = responseBody();
            body.isSuccess = false;
            body.isValid = false;
            body.validationResults = [{ severity: 2, message: 'Stale example error', members: ['code'], state: {} }];
            pending.resolve(httpResponse(body));
        });
        expect(context.getFieldError('code')).to.equal('Current example error');
        expect(handle.current!.isValid).to.equal(false);
        await act(async () => { context.setCustomFieldError('code', undefined); });
        expect(context.getFieldError('code')).to.equal(undefined);
        expect(context.isValid).to.equal(true);
        expect(handle.current!.isValid).to.equal(true);
    });

    it('does not corrupt overlapping native execution counts with a blocked attempt', async () => {
        const first = pendingResponse();
        const second = pendingResponse();
        fetchStub.onFirstCall().returns(first.promise);
        fetchStub.onSecondCall().returns(second.promise);
        await mount();
        let firstExecution!: Promise<ICommandResult<unknown>>;
        let secondExecution!: Promise<ICommandResult<unknown>>;
        await act(async () => {
            firstExecution = handle.current!.execute();
            secondExecution = handle.current!.execute();
            context.setCustomFieldError('code', 'Example error');
            expect((await handle.current!.execute()).isValid).to.equal(false);
            expect(handle.current!.isExecuting).to.equal(true);
        });
        expect(fetchStub.callCount).to.equal(2);
        expect(context.isExecuting).to.equal(true);
        await act(async () => { first.resolve(httpResponse()); await firstExecution; });
        expect(handle.current!.isExecuting).to.equal(true);
        expect(context.isExecuting).to.equal(true);
        await act(async () => { second.resolve(httpResponse()); await secondExecution; });
        expect(handle.current!.isExecuting).to.equal(false);
        expect(context.isExecuting).to.equal(false);
        expect(handle.current!.isValid).to.equal(false);
        expect(success.callCount).to.equal(2);
        expect(failed.callCount).to.equal(1);
    });

    it('preserves native required validation after custom errors clear', async () => {
        await mount({ command: ExampleRequiredCommand });
        await act(async () => {
            context.setCustomFieldError('code', 'Example format error');
            await handle.current!.execute();
            context.setCustomFieldError('code', undefined);
            const result = await handle.current!.execute();
            expect(result.validationResults[0].message).to.equal('code is required');
            expect(result.validationResults[0].members).to.deep.equal(['code']);
        });
        expect(fetchStub.callCount).to.equal(0);
        expect(validationFailure.callCount).to.equal(2);
        expect(handle.current!.isValid).to.equal(false);
    });

    for (const failure of ['authorization', 'validation', 'exception'] as const) {
        it(`preserves native server ${failure} results and callbacks once custom errors clear`, async () => {
            const body = responseBody();
            body.isSuccess = false;
            if (failure === 'authorization') {
                body.isAuthorized = false;
                body.authorizationFailureReason = 'Example role required';
            } else if (failure === 'validation') {
                body.isValid = false;
                body.validationResults = [{ severity: 2, message: 'Example server rule', members: ['code'], state: {} }];
            } else {
                body.hasExceptions = true;
                body.exceptionMessages = ['Example diagnostic'];
            }
            fetchStub.callsFake(async () => httpResponse(body));
            await mount({ command: ExampleRestrictedCommand });
            expect(handle.current!.isAuthorized).to.equal(false);
            await act(async () => {
                context.setCustomFieldError('code', 'Example local error');
                await handle.current!.execute();
                context.setCustomFieldError('code', undefined);
                const result = await handle.current!.execute();
                expect(result).to.be.instanceOf(CommandResult);
                expect(result.isSuccess).to.equal(false);
            });
            expect(fetchStub.callCount).to.equal(1);
            expect(failed.callCount).to.equal(2);
            expect(unauthorized.callCount).to.equal(failure === 'authorization' ? 1 : 0);
            expect(validationFailure.callCount).to.equal(failure === 'validation' ? 2 : 1);
            expect(exception.callCount).to.equal(failure === 'exception' ? 1 : 0);
            expect(success.callCount).to.equal(0);
            expect(handle.current!.isAuthorized).to.equal(false);
        });
    }

    it('keeps the raw Command.execute contract independent of form-local errors', async () => {
        await mount();
        await act(async () => {
            context.setCustomFieldError('code', 'Example form-local error');
            expect((await context.commandInstance.execute()).isSuccess).to.equal(true);
        });
        expect(fetchStub.callCount).to.equal(1);
        expect(handle.current!.isValid).to.equal(false);
        expect(success.callCount).to.equal(0);
    });
});
