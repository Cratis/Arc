// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="vitest/globals" />
/// <reference types="chai/register-should" />
/// <reference types="sinon" />
/// <reference types="sinon-chai" />
/// <reference types="mocha" />

declare global {
    const describe: Mocha.SuiteFunction;
    const it: Mocha.TestFunction;
    const beforeEach: Mocha.HookFunction;
    const afterEach: Mocha.HookFunction;
    const before: Mocha.HookFunction;
    const after: Mocha.HookFunction;

    namespace Chai {
        interface Assertion {
            called: Assertion;
        }
    }
}

export {};
