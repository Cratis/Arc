// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import * as chai from 'chai';
chai.should();
import * as chaiAsPromised from 'chai-as-promised';
import * as sinonChai from 'sinon-chai';

// Vitest 4.x (@vitest/expect) adds sinon-compatible chainable methods (callCount, calledWith, etc.)
// as getter-only properties on chai.Assertion.prototype. sinon-chai uses addMethod (simple assignment)
// to override these, which fails in strict mode. Delete them first so sinon-chai can define them properly.
const sinonChaiMethodNames = ['callCount', 'calledBefore', 'calledAfter', 'calledWith', 'calledOnceWith', 'returned'];
for (const name of sinonChaiMethodNames) {
    delete (chai.Assertion.prototype as Record<string, unknown>)[name];
}

chai.use(sinonChai.default);
chai.use(chaiAsPromised.default);

import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

afterEach(() => {
    cleanup();
});
