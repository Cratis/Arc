// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, cleanup } from '@testing-library/react';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import sinon from 'sinon';
import { CommandForm, useCommandFormContext, useSetCommandResult } from '../CommandForm.js';
import { CommandFormDefaults } from '../CommandFormDefaults.js';
import { InputTextField } from '../fields/InputTextField.js';
import type { ValidationSummaryProps } from '../ValidationSummary.js';
import { given } from '../../../given.js';
import { a_command_form_with_exception_feedback } from './given/a_command_form_with_exception_feedback.js';
import { TestCommand } from './TestCommand.js';

const failure = (message: string, members: string[] = []) => new ValidationResult(ValidationResultSeverity.Error, message, members, undefined);

describe('when displaying unbound validation', given(a_command_form_with_exception_feedback, context => {
    afterEach(() => cleanup());
    beforeEach(async () => {
        await context.renderForm({ children: <InputTextField<TestCommand> value={command => command.name} title='Name' /> });
        await act(async () => context.setResult(context.exceptionResult(false, [], [
            failure('Quota exceeded'), failure('Missing field', ['email']), failure('Name invalid', ['name.Value']),
            failure('Shared rule', ['email', 'name']), failure('Quota exceeded'), failure(' '),
        ])));
    });
    it('should announce only unmatched messages once before fields', () => {
        const alert = context.rendered.getByRole('alert');
        alert.textContent!.should.equal('Quota exceededMissing field');
        (context.rendered.container.querySelector('form')!.firstElementChild === alert).should.equal(true);
    });
    it('should preserve the existing field feedback', () => context.formContext.getFieldError('name')!.should.equal('Name invalid'));
    it('should clear the summary when a successful result replaces it', async () => {
        await act(async () => context.setResult(context.exceptionResult(false, [], [])));
        context.rendered.queryAllByRole('alert').length.should.equal(0);
    });
    it('should keep exception feedback separate and safe', async () => {
        await act(async () => context.setResult(context.exceptionResult(true, [context.privateMessage], [failure('Quota exceeded')])));
        context.rendered.getAllByRole('alert').length.should.equal(2);
        context.rendered.container.textContent!.should.not.contain(context.privateMessage);
    });
}));

describe('when overriding unbound validation', given(a_command_form_with_exception_feedback, context => {
    let display: sinon.SinonSpy;
    beforeEach(() => { display = sinon.spy((props: ValidationSummaryProps) => { void props; return null; }); });
    afterEach(() => cleanup());
    it('should not invoke a replacement when errors are hidden', async () => {
        await context.renderForm({ showErrors: false, validationSummaryComponent: display });
        await act(async () => context.setResult(context.exceptionResult(false, [], [failure('Quota exceeded')])));
        display.callCount.should.equal(0);
    });
    it('should pass only unmatched results to a null-rendering replacement', async () => {
        const results = [failure('Quota exceeded')];
        await context.renderForm({ validationSummaryComponent: display });
        await act(async () => context.setResult(context.exceptionResult(false, [], results)));
        display.lastCall.args[0].should.deep.equal({ validationResults: results });
        context.rendered.queryAllByRole('alert').length.should.equal(0);
    });
}));

describe('when registered fields change', given(a_command_form_with_exception_feedback, context => {
    let setResult: ReturnType<typeof useSetCommandResult>;
    let formContext: ReturnType<typeof useCommandFormContext<TestCommand>>;
    const Probe = () => { setResult = useSetCommandResult(); formContext = useCommandFormContext<TestCommand>(); return null; };
    const field = (email: boolean) => <InputTextField<TestCommand> value={email ? command => command.email : command => command.name} />;
    const form = (count: number, email = false) => <CommandFormDefaults value={{ validationSummaryComponent: () => <aside role='alert'>Provider</aside> }}>
        <CommandForm command={TestCommand} validationSummaryComponent={undefined}>
            <Probe /><section>{count > 0 && field(email)}{count > 1 && field(email)}</section>
        </CommandForm>
    </CommandFormDefaults>;
    afterEach(() => cleanup());
    it('should track duplicate nested registrations, removals and changed accessors', async () => {
        const result = render(form(2), { wrapper: context.createWrapper() });
        await act(async () => setResult(context.exceptionResult(false, [], [failure('Name invalid', ['name'])])));
        result.queryAllByRole('alert').length.should.equal(0);
        result.rerender(form(1));
        result.queryAllByRole('alert').length.should.equal(0);
        result.rerender(form(1, true));
        result.getByRole('alert').textContent!.should.equal('Provider');
        result.rerender(form(1));
        result.queryAllByRole('alert').length.should.equal(0);
        result.rerender(form(0));
        result.getByRole('alert').textContent!.should.equal('Provider');
        formContext.getFieldError('name')!.should.equal('Name invalid');
    });
}));
