// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="vitest/globals" />
/// <reference types="chai" />
/// <reference types="sinon-chai" />

import * as React from 'react';

declare global {
  namespace JSX {
    interface Element extends React.ReactElement {}
    // Add additional JSX-related overrides if necessary.
  }
  
  // Ensure chai should is available globally
  namespace Chai {
    interface Assertion extends LanguageChains, NumericComparison, TypeComparison {
      called: Assertion;
    }
  }
}