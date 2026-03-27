// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="vitest/globals" />
/// <reference types="chai/register-should" />
/// <reference types="sinon-chai" />

// Declare the global object for test environment
declare const global: {
    document?: Document;
    [key: string]: any;
};

// Ensure chai should is available globally
declare global {
    namespace Chai {
        interface Assertion extends LanguageChains, NumericComparison, TypeComparison {
            called: Assertion;
        }
    }
}

export {};
