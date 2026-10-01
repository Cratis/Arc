// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, render } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

const editor = vi.hoisted(() => {
    let value = '';
    return {
        getValue: vi.fn(() => value),
        setValue: vi.fn((next: string) => { value = next; }),
        getModel: vi.fn(() => ({ dispose: vi.fn() })),
        dispose: vi.fn()
    };
});
const create = vi.hoisted(() => vi.fn(() => editor));
const register = vi.hoisted(() => vi.fn());

vi.mock('monaco-editor', () => ({ editor: { create } }));
vi.mock('monaco-editor/editor/editor.worker?worker', () => ({ default: class { } }));
vi.mock('@cratis/screenplay-language', () => ({ languageId: 'screenplay', screenplayDarkThemeName: 'screenplay-dark', register }));

import { ScreenplayEditor } from '../ScreenplayEditor';

// Registering happens once, when the editor module loads.
const registrationsOnLoad = register.mock.calls.length;

describe('when showing source', () => {
    afterEach(() => cleanup());

    it('registers the Screenplay language with Monaco', () => {
        expect(registrationsOnLoad).toBe(1);
    });

    it('shows it read-only, highlighted as Screenplay in the Screenplay theme', () => {
        render(<ScreenplayEditor value='feature Checkout {}' />);

        expect(create).toHaveBeenCalledWith(expect.any(HTMLDivElement), expect.objectContaining({
            language: 'screenplay',
            theme: 'screenplay-dark',
            readOnly: true,
            domReadOnly: true
        }));
        expect(editor.getValue()).toBe('feature Checkout {}');
    });

    it('shows new source without recreating the editor', () => {
        create.mockClear();
        const { rerender } = render(<ScreenplayEditor value='feature Checkout {}' />);

        rerender(<ScreenplayEditor value='feature Payment {}' />);

        expect(create).toHaveBeenCalledOnce();
        expect(editor.getValue()).toBe('feature Payment {}');
    });

    it('disposes the editor when it is no longer shown', () => {
        editor.dispose.mockClear();
        const { unmount } = render(<ScreenplayEditor value='feature Checkout {}' />);

        unmount();

        expect(editor.dispose).toHaveBeenCalledOnce();
    });
});
