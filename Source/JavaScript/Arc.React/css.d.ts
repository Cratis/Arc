// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Stylesheets are side-effect imported by the components that need them, so that a consumer's
// bundler pulls them in on its own. The bundler is what resolves the specifier; the type system
// only has to stop treating it as a missing module.
//
// This file deliberately has no top-level import or export - that is what keeps it a global
// script rather than a module, which is the only place an ambient module declaration is allowed.
declare module '*.css';
