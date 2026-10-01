// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Alias } from 'vite';

/**
 * Monaco carries its own copy of DOMPurify, imported as './dompurify/dompurify.js' from
 * vs/base/browser/domSanitize.js. Monaco pins a version with known advisories and updates it only with
 * its own releases, so the viewer resolves that import to the dompurify package it depends on directly,
 * which it can keep patched. Both export DOMPurify as their default export.
 */
export const vendoredDomPurify: Alias = { find: /^\.\/dompurify\/dompurify\.js$/, replacement: 'dompurify' };

/**
 * The Screenplay language brings its own tokenizer, so only Monaco's core editor is needed - not the
 * dozens of languages the bare 'monaco-editor' entry registers.
 */
export const monacoCoreEditor: Alias = { find: /^monaco-editor$/, replacement: 'monaco-editor/editor/editor.api' };

export const monacoAliases: Alias[] = [monacoCoreEditor, vendoredDomPurify];
