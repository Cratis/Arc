---
title: Cratis Arc Storybook theme wiring
description: Cratis Arc story components use data-theme to select dark or light colors. Connect that attribute to your Storybook background and custom styles.
---

Cratis Arc story components select dark or light colors through the `data-theme` attribute. After loading the stylesheet, set this attribute to match your Storybook canvas background. The stylesheet does not detect Storybook's background or the operating system color scheme on its own.

## How it works

Load the stylesheet once in your Storybook preview, as shown in [Getting started](index.md#getting-started). It defines CSS custom properties, which are named values such as `--color-text` that components read through `var(...)`. Selecting a palette changes the values inherited by those components.

The translucent badge backgrounds use fixed `rgba(...)` values instead. Override `.story-badge-*` if you need those backgrounds to match a custom palette.

### Default theme

Without a `data-theme` attribute, the stylesheet uses the dark palette.

### Switching themes

Set `data-theme` on `<html>` or `<body>`. `data-theme="light"` selects the light palette, `data-theme="dark"`
selects the dark one, and no attribute at all leaves you with dark. Because the properties are inherited, the
attribute can also sit on a container to theme one part of a page:

```html
<script>
    document.documentElement.setAttribute('data-theme', 'light');
</script>
```

Keep palette selection in the configuration that controls your canvas background. A light palette chosen from the operating system setting could put dark text on a dark Storybook canvas if the toolbar selection differs.

For background-driven switching, the Arc repository's Storybook includes a script in `.storybook/preview-head.html`, under `Source/JavaScript/Arc.React`. It reads the body's computed background color, estimates its brightness, and sets `data-theme` on the body. The script observes style and class changes and also checks periodically.

To use the same wiring, copy that script into your project's `.storybook/preview-head.html`. It is repository configuration, not part of the package stylesheet. When you change the canvas background, check that the body's `data-theme` attribute changes with it.

## Theme-aware design

### Color adaptation

The dark palette supplies lighter text and darker backgrounds; the light palette supplies darker text and lighter backgrounds. Success, warning, error, and info text colors also have separate values for each palette. Check contrast in the context of your story, especially when overriding colors.

### Automatic adjustments

Changing `data-theme` updates the inherited text, background, border, and status color variables. A card reads its background from `--color-background-secondary`, so that background follows the palette without a conditional in React. Spacing, radii, and shadows stay the same in both palettes, as do the primary and primary-hover color values.

## Developer experience

<a id="no-additional-code-required"></a>

### Use the selected palette

Once your Storybook configuration sets `data-theme`, components that use the variables follow it without their own switching logic:

```tsx
// This automatically works in both themes
<StoryContainer asCard>
    <h1>My Component</h1>
    <MyComponent />
</StoryContainer>
```

### Theme-safe custom styles

Use the same variables in custom story content. The first example reads its text and background colors from the selected palette; the second keeps the same colors in both themes:

```tsx
// ✅ Good - Uses CSS variables
<div style={{
  color: 'var(--color-text)',
  backgroundColor: 'var(--color-background-secondary)'
}}>
  Theme-safe content
</div>

// ❌ Avoid - Hard-coded colors
<div style={{
  color: '#ffffff',
  backgroundColor: '#1a1a1a'
}}>
  Only works in dark mode
</div>
```

## Testing your stories

### Always test both themes

Check the story after wiring palette selection:

1. Open it with the default dark palette and inspect the text, cards, and borders.
2. Set `data-theme="light"`, or select a light background if you connected the toolbar, and confirm the palette changes.
3. Check text contrast against the backgrounds your story actually uses in both palettes.
4. Inspect hover, focus, and active states in each palette.

### Common issues and solutions

#### Issue: Poor contrast in one theme

If text is readable in dark mode but not in light mode, look for a fixed color in your custom styles. Use a text color variable so it can follow the palette:

```tsx
// Instead of:
<p style={{ color: '#888' }}>Text</p>

// Use:
<p style={{ color: 'var(--color-text-secondary)' }}>Text</p>
```

#### Issue: Borders not visible

If a border disappears or becomes too prominent after switching themes, use the border color variable instead of a fixed shade:

```tsx
// Instead of:
<div style={{ border: '1px solid #333' }}>Content</div>

// Use:
<div style={{ border: '1px solid var(--color-border)' }}>Content</div>
```

#### Issue: Custom colors don't adapt

Custom colors need their own values under the dark and light selectors if they must change with the palette. The primary color variable is available for shared accents, but its value is the same in both palettes. In this example, the button text changes through `--color-background`; the primary background color stays the same:

```tsx
// For brand colors that should adapt:
<button
    style={{
        backgroundColor: 'var(--color-primary)',
        color: 'var(--color-background)', // Inverts with theme
    }}
>
    Button
</button>
```

## Best practices

Use variables named for their role, such as `--color-text` and `--color-background`, so custom content follows the surrounding story. If a component intentionally looks different in one theme, explain that behavior in its story so readers know what to expect.

## Implementation details

The palette comes from the stylesheet exported as `@cratis/arc.react/stories/styles.css`. Dark color properties are declared on `:root` and `[data-theme='dark']`; light properties are declared on `[data-theme='light']`. Inheritance lets you select a palette on the page or on a smaller container, including a nested region with a different theme.

The stylesheet needs no React theme provider. Your Storybook configuration still owns setting the attribute, whether directly or through the background-watching script. For the available variables and overrides, continue to the [styling reference](styling.md).
