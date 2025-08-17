# TradeDataHub Theme System

This directory contains the modular theme system for TradeDataHub. The theme has been refactored from a single large file into organized, maintainable modules.

## Structure

```
Themes/
├── LightTheme.xaml              # Main theme file that imports all modules
├── Foundation/                  # Core design tokens and foundations
│   ├── Colors.xaml             # Color palette definitions
│   ├── Brushes.xaml            # Brush resources based on colors
│   ├── Typography.xaml         # Font families, sizes, and text styles
│   └── Tokens.xaml             # Design tokens (spacing, shadows, etc.)
├── Controls/                    # Control-specific styles
│   ├── Buttons.xaml            # Button styles (Primary, Success, Danger, etc.)
│   ├── Inputs.xaml             # Input controls (TextBox, ComboBox, ListView)
│   ├── Toggles.xaml            # Toggle controls and switches
│   └── CustomControls.xaml     # Custom control styles (YearMonthPicker, MonitoringPanel)
├── Layout/                      # Layout and container styles
│   └── Containers.xaml         # Panel and container styles
└── Animations/                  # Animation styles
    └── Animations.xaml         # Fade, slide, and scale animations
```

## Benefits of Modular Structure

### 1. **Maintainability**
- Each file focuses on a specific concern
- Easy to locate and modify specific styles
- Reduced file size makes editing more manageable

### 2. **Reusability**
- Individual modules can be imported separately if needed
- Foundation modules can be shared across different themes
- Control styles can be easily extended or overridden

### 3. **Organization**
- Logical grouping of related styles
- Clear separation of concerns
- Easier for team collaboration

### 4. **Performance**
- Only load what you need
- Better resource management
- Faster compilation times

## Usage

### Importing the Complete Theme
The main `LightTheme.xaml` file imports all modules:

```xml
<ResourceDictionary Source="Themes/LightTheme.xaml"/>
```

### Importing Individual Modules
You can import specific modules if needed:

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Themes/Foundation/Colors.xaml"/>
    <ResourceDictionary Source="Themes/Controls/Buttons.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

## Key Style Categories

### Foundation Styles
- **Colors**: Primary, neutral, and semantic color definitions
- **Brushes**: Solid color brushes and gradients based on colors
- **Typography**: Font families, sizes, weights, and text block styles
- **Tokens**: Spacing, corner radius, shadows, and animation settings

### Control Styles
- **Buttons**: ModernButton, PrimaryButton, SuccessButton, DangerButton, NeutralButton
- **Inputs**: ModernTextBox, ModernComboBox, ModernToggleButton, ModernLogEntry
- **Toggles**: ModernToggleSwitch, ToggleOption, ToggleText
- **CustomControls**: YearMonthPicker styles, MonitoringPanel styles, Search components

### Layout Styles
- **Containers**: ExpandablePanel, CollapsibleContent

### Animation Styles
- **Animations**: FadeInAnimation, SlideInFromRightAnimation, ScaleUpAnimation

### Application-Specific Styles
- **ModernInfoCard**: Card-style container with hover effects
- **ModernStatusBadge**: Status indicator badge

## Adding New Styles

### 1. Choose the Right Module
- **Foundation**: Core design elements (colors, fonts, spacing)
- **Controls**: Specific control styles
- **Layout**: Container and panel styles
- **Animations**: Motion and transition styles

### 2. Follow Naming Conventions
- Use descriptive, hierarchical names
- Prefix with category (e.g., `Color.Primary`, `Brush.Text.Primary`)
- Use consistent naming patterns

### 3. Maintain Dependencies
- Ensure proper ResourceDictionary imports
- Reference foundation styles in control styles
- Test that all dependencies are resolved

## Migration Notes

The original `LightTheme.xaml` (750+ lines) has been split into:
- **Colors.xaml**: 32 lines
- **Brushes.xaml**: 45 lines  
- **Typography.xaml**: 85 lines
- **Tokens.xaml**: 35 lines
- **Buttons.xaml**: 285 lines
- **Inputs.xaml**: 115 lines
- **Toggles.xaml**: 55 lines
- **CustomControls.xaml**: 140 lines
- **Containers.xaml**: 60 lines
- **Animations.xaml**: 75 lines
- **LightTheme.xaml**: 80 lines (main import file)

Total: ~1000 lines (organized and maintainable vs. 750+ lines in single file)

## Best Practices

1. **Keep modules focused**: Each file should have a single responsibility
2. **Use consistent naming**: Follow the established naming conventions
3. **Document changes**: Update this README when adding new modules
4. **Test thoroughly**: Ensure all style references work after changes
5. **Maintain dependencies**: Keep import order correct in main theme file

## Troubleshooting

### Style Not Found Errors
1. Check that the module containing the style is imported
2. Verify the style name is correct
3. Ensure dependency chain is complete (Colors → Brushes → Controls)

### Circular Dependencies
1. Keep foundation modules independent
2. Only reference foundation styles in control styles
3. Avoid cross-references between control modules

### Performance Issues
1. Only import modules you actually use
2. Consider lazy loading for large applications
3. Monitor resource dictionary size in memory profiler