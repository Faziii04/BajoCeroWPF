# Theme Fix + DataGrid Cleanup Plan

## Issue 1: Hardcoded White Text in Sidebar (Dashboard.xaml)

Elements that stay white after switching to Light mode:

| Element | Current | Fix |
|---------|---------|-----|
| "BajoCero" title | `Foreground="White"` | `Foreground="{DynamicResource NavTextColor}"` |
| "Panel de control" subtitle | Already dynamic (OK) | — |
| User name (txtNombreUsuario) | `Foreground="White"` | `Foreground="{DynamicResource NavTextColor}"` |
| User role badge text | `Foreground="White"` (OK — badge has accent bg) | — |
| User email (txtEmailUsuario) | `Foreground="White"` | `Foreground="{DynamicResource NavTextColor}"` + `Opacity="0.5"` |
| User card background | `#15FFFFFF` (semi-transparent white) | `{DynamicResource POscuro2}` with opacity |
| Decorative ellipses | `Fill="#FFFFFF"` | `Fill="{DynamicResource NavTextColor}"` |

## Issue 2: DistribucionUC Cleanup

- Remove all local `ResourceDictionary` styles (now in SharedStyles via themes)
- Add `CanUserAddRows="False"` to DataGrid (kills "ghost" empty row)
- Change from `MouseDoubleClick` to single-click `SelectionChanged`
- Remove empty `dgVentas_SelectionChanged` method

## Issue 3: EmpleadosUC Cleanup

- Remove all local `ResourceDictionary` styles
- Add `CanUserAddRows="False"` to DataGrid
- Change from `MouseDoubleClick` to single-click `SelectionChanged`
- Remove "Editar" button (replaced by single-click row)
- Fix placeholder `Foreground="#888"` → `{DynamicResource PlaceholderTextBrush}`
