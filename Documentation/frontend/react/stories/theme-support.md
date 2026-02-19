# Automatic Theme Support

All story components and CSS variables automatically adapt to the current Storybook theme, providing seamless dark and light mode support.

## How It Works

The story components use CSS custom properties (variables) that are defined differently for each theme. When you switch themes in Storybook using the backgrounds toolbar, the CSS variables automatically update, causing all styles to adjust accordingly.

### Default Theme

The default theme is **dark mode**. This ensures a consistent baseline experience when viewing stories.

### Switching Themes

Use the **backgrounds toolbar** in Storybook to switch between dark and light modes:

1. Click the backgrounds icon in the Storybook toolbar
2. Select either the dark or light background option
3. All story components will instantly adapt to the new theme

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

You don't need to write any theme-switching logic. Simply use the provided components and CSS variables:

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
2. **Switch to Light Mode**: Check that all elements remain readable and visually appealing
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

- CSS custom properties scoped to the document or Storybook root
- Theme detection based on Storybook's background addon
- Automatic variable updates when the theme changes

Developers using the story components don't need to understand these implementation details—the system works transparently.
