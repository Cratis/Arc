# Automatic Theme Support

All story components and CSS variables adapt to the theme in effect, providing seamless dark and light mode support.

## How It Works

Every color in the story components is read from a CSS custom property rather than written into a rule.
The stylesheet that ships with the package declares those properties twice - once for dark, once for light -
so changing the theme changes one attribute and every component follows.

### Default Theme

The default theme is **dark mode**. This ensures a consistent baseline experience when viewing stories.

### Switching Themes

Set `data-theme` on `<html>` or `<body>`. `data-theme="light"` selects the light palette, `data-theme="dark"`
selects the dark one, and no attribute at all leaves you with dark. Because the properties are inherited, the
attribute can also sit on a container to theme one part of a page:

```html
<script>
  document.documentElement.setAttribute('data-theme', 'light');
</script>
```

The package deliberately does **not** read the operating system color scheme on its own. The palette has to
agree with whatever paints the canvas behind it - the Storybook background, your own preview decorator - and
only your Storybook configuration knows what that is. Deciding for you would produce dark text on a dark
canvas for anyone whose machine disagreed with their toolbar.

The Arc repository's own Storybook wires the two together: a snippet in its `.storybook/preview-head.html`
watches the canvas background, measures its luminance, and sets `data-theme` to match - so the backgrounds
toolbar drives the theme. Copy that approach if you want the same behavior in yours.

## Theme-Aware Design

### Color Adaptation

Colors are the primary difference between themes:

- **Dark Mode**: Uses lighter text on darker backgrounds
- **Light Mode**: Uses darker text on lighter backgrounds

All semantic colors (success, warning, error, info) have theme-appropriate variants to ensure proper contrast and readability.

### Automatic Adjustments

When switching themes, the following automatically update:

- **Text Colors**: Primary, secondary, and muted text colors adjust for readability
- **Background Colors**: Container, card, and input backgrounds adapt
- **Border Colors**: Borders become lighter or darker based on the theme
- **Shadows**: Box shadows are optimized for each theme
- **Status Colors**: Success, warning, error, and info colors maintain proper contrast

## Developer Experience

### No Additional Code Required

Beyond importing the stylesheet once and deciding when `data-theme` flips, you don't write any
theme-switching logic. Simply use the provided components and CSS variables:

```tsx
// This automatically works in both themes
<StoryContainer asCard>
  <h1>My Component</h1>
  <MyComponent />
</StoryContainer>
```

### Theme-Safe Custom Styles

When adding custom styles, use CSS variables to ensure theme compatibility:

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

## Testing Your Stories

### Always Test Both Themes

To ensure your stories look great in all contexts:

1. **Start in Dark Mode**: Verify your story looks correct in the default theme
2. **Switch to Light Mode**: Set `data-theme="light"` and check that all elements remain readable and visually appealing
3. **Check Color Contrast**: Ensure text is legible against backgrounds in both themes
4. **Verify Interactive States**: Hover, focus, and active states should work in both themes

### Common Issues and Solutions

#### Issue: Poor Contrast in One Theme

**Problem**: Text is hard to read in light mode but fine in dark mode.

**Solution**: Use semantic text color variables instead of hard-coded values:

```tsx
// Instead of:
<p style={{ color: '#888' }}>Text</p>

// Use:
<p style={{ color: 'var(--color-text-secondary)' }}>Text</p>
```

#### Issue: Borders Not Visible

**Problem**: Borders disappear or become too prominent when switching themes.

**Solution**: Use border color variables:

```tsx
// Instead of:
<div style={{ border: '1px solid #333' }}>Content</div>

// Use:
<div style={{ border: '1px solid var(--color-border)' }}>Content</div>
```

#### Issue: Custom Colors Don't Adapt

**Problem**: Brand or custom colors don't change with the theme.

**Solution**: Define theme-specific versions of your custom colors or use the primary color variable:

```tsx
// For brand colors that should adapt:
<button style={{ 
  backgroundColor: 'var(--color-primary)',
  color: 'var(--color-background)' // Inverts with theme
}}>
  Button
</button>
```

## Best Practices

1. **Always Use Variables**: Prefer CSS variables over hard-coded color values
2. **Test Both Themes**: Verify your stories in dark and light modes before committing
3. **Semantic Colors**: Use semantic variables (`--color-text`, `--color-background`) instead of specific shades
4. **Document Theme Considerations**: If a component has theme-specific behavior, document it
5. **Consistent Patterns**: Follow the patterns established by existing story components

## Implementation Details

The theme system is implemented using:

- A stylesheet published inside `@cratis/arc.react`, reached through the `@cratis/arc.react/stories/styles.css`
  export and imported once from your Storybook preview
- CSS custom properties declared on `:root` for the dark palette and on `[data-theme='light']` for the light
  one, which is why the attribute can sit on any ancestor and why your own CSS can override any of them
- Nothing else - no runtime, no theme provider, no build step

Developers using the story components don't need to understand these implementation details—the system works transparently.
