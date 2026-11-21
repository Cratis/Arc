// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act, cleanup } from '@testing-library/react';
import sinon from 'sinon';
import { CommandForm } from '../CommandForm.js';
import { CommandFormDefaults } from '../CommandFormDefaults.js';
import { useCommandFormContext, type CommandFormContextValue } from '../CommandFormContext.js';
import { TestCommand } from './TestCommand.js';
import { a_command_form_context } from './given/a_command_form_context.js';

let context: CommandFormContextValue<unknown>;
const Probe = () => { context = useCommandFormContext(); return null; };
const form = (props = {}) => <CommandForm command={TestCommand} {...props}><Probe /></CommandForm>;

describe('when using defaults', () => {
    afterEach(() => { cleanup(); sinon.restore(); });

    describe('without a provider', () => {
        beforeEach(() => { render(form(), { wrapper: new a_command_form_context().createWrapper() }); });
        it('should retain built-in defaults', () => {
            context.showTitles.should.equal(true);
            context.showErrors.should.equal(true);
            context.validateOn.should.equal('blur');
            context.autoServerValidate.should.equal(false);
            context.autoServerValidateThrottle.should.equal(500);
        });
    });

    describe('with nested providers and explicit overrides', () => {
        beforeEach(() => {
            render(<CommandFormDefaults value={{ showTitles: false, showErrors: false, validateOn: 'change', autoServerValidateThrottle: 900, errorClassName: 'outer' }}>
                <CommandFormDefaults value={{ showTitles: undefined, showErrors: true }}>
                    {form({ showErrors: false, autoServerValidateThrottle: 0, errorClassName: '' })}
                </CommandFormDefaults>
            </CommandFormDefaults>, { wrapper: new a_command_form_context().createWrapper() });
        });
        it('should inherit defined settings while preserving false zero and empty string', () => {
            context.showTitles.should.equal(false);
            context.showErrors.should.equal(false);
            context.validateOn.should.equal('change');
            context.autoServerValidateThrottle.should.equal(0);
            context.errorClassName!.should.equal('');
        });
    });

    describe('with undefined instance settings and untouched command values', () => {
        beforeEach(async () => {
            await act(async () => { render(<CommandFormDefaults value={{ showTitles: false, exceptionMessage: 'Safe message' }}>
                <CommandForm<TestCommand> command={TestCommand} showTitles={undefined} initialValues={{ name: 'initial' }} currentValues={{ name: 'current' }}>
                    <Probe />
                </CommandForm>
            </CommandFormDefaults>, { wrapper: new a_command_form_context().createWrapper() }); });
        });
        it('should inherit undefined settings without changing value precedence', () => {
            context.showTitles.should.equal(false);
            (context.commandInstance as TestCommand).name!.should.equal('initial');
        });
    });

    describe('with changing provider values', () => {
        beforeEach(() => {
            const result = render(<CommandFormDefaults value={{ showTitles: false }}>{form()}</CommandFormDefaults>, { wrapper: new a_command_form_context().createWrapper() });
            result.rerender(<CommandFormDefaults value={{ showTitles: true }}>{form()}</CommandFormDefaults>);
        });
        it('should update mounted forms', () => { context.showTitles.should.equal(true); });
    });

    describe('with provider-driven initial server validation', () => {
        let validate: sinon.SinonStub;
        beforeEach(async () => {
            validate = sinon.stub(TestCommand.prototype, 'validate').resolves({ isValid: true, validationResults: [] } as never);
            await act(async () => {
                render(<CommandFormDefaults value={{ validateOnInit: true, autoServerValidate: true }}>{form()}</CommandFormDefaults>, { wrapper: new a_command_form_context().createWrapper() });
            });
        });
        it('should apply defaults to actual validation hooks', () => { validate.callCount.should.be.greaterThan(0); });
    });

    describe('with scoped component defaults', () => {
        const Outer = () => null;
        const Inner = () => null;
        const observed: unknown[] = [];
        const Observe = () => { observed.push(useCommandFormContext().errorDisplayComponent); return null; };
        beforeEach(() => {
            observed.length = 0;
            render(<>
                <CommandFormDefaults value={{ errorDisplayComponent: Outer }}>
                    <CommandForm command={TestCommand} errorDisplayComponent={Inner}><Observe /></CommandForm>
                </CommandFormDefaults>
                <CommandForm command={TestCommand}><Observe /></CommandForm>
            </>, { wrapper: new a_command_form_context().createWrapper() });
        });
        it('should prefer the instance component without leaking to siblings', () => {
            observed.should.include(Inner);
            observed.includes(Outer).should.equal(false);
            observed.includes(undefined).should.equal(true);
        });
    });
});
