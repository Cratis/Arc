// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { createFieldBlurHandler, createFieldValueChangeHandler, type CommandFormFieldHandlerScope } from '../commandFormFieldHandlers.js';

const issue = 7;

const scopeThatThrows = (validateOn: 'blur' | 'change') => {
    const failSilentValidation = sinon.spy();
    const beginSilentValidation = sinon.stub().returns(issue);
    const commandInstance = {
        validateClientSide: () => { throw new Error('validator exploded'); },
    };
    const scope = {
        context: {
            commandInstance,
            validateOn,
            autoServerValidate: false,
            setCommandValues: () => { },
            setCustomFieldError: () => { },
            beginSilentValidation,
            setSilentValidationResult: () => true,
            failSilentValidation,
        },
        propertyName: 'name',
        fieldProps: {},
        currentValue: 'a',
    } as unknown as CommandFormFieldHandlerScope;
    return { scope, failSilentValidation, beginSilentValidation };
};

describe('when the validation after a value change throws', () => {
    let failSilentValidation: sinon.SinonSpy;
    let beginSilentValidation: sinon.SinonStub;
    let thrown: unknown;

    beforeEach(async () => {
        const arranged = scopeThatThrows('change');
        failSilentValidation = arranged.failSilentValidation;
        beginSilentValidation = arranged.beginSilentValidation;
        thrown = undefined;
        try {
            await createFieldValueChangeHandler(arranged.scope)('b');
        } catch (error) {
            thrown = error;
        }
    });

    it('should claim a token for the client channel', () => beginSilentValidation.calledWith('client').should.be.true);

    it('should report the validation as failed under its token', () => failSilentValidation.calledOnceWithExactly(issue).should.be.true);

    it('should not swallow the failure', () => (thrown as Error).message.should.equal('validator exploded'));
});

describe('when the validation on blur throws', () => {
    let failSilentValidation: sinon.SinonSpy;
    let thrown: unknown;

    beforeEach(async () => {
        const arranged = scopeThatThrows('blur');
        failSilentValidation = arranged.failSilentValidation;
        thrown = undefined;
        try {
            await createFieldBlurHandler(arranged.scope)();
        } catch (error) {
            thrown = error;
        }
    });

    it('should report the validation as failed under its token', () => failSilentValidation.calledOnceWithExactly(issue).should.be.true);

    it('should not swallow the failure', () => (thrown as Error).message.should.equal('validator exploded'));
});
