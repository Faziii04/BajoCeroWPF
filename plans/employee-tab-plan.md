# Plan: Add Employees Tab to Dashboard + EmpleadosUC User Control

## Overview

Add a new "Empleados" navigation tab to the Dashboard sidebar and implement the `EmpleadosUC` user control with full CRUD functionality for the `empleado` table, including role assignment via `empleado_rol`.

---

## Step 1: Create `EmpleadoModel.cs` in `Models/`

A plain model class mapping to the `empleado` table columns:

| Property       | Type     | DB Column     |
|----------------|----------|---------------|
| Ci             | string   | ci (PK)       |
| Nombre         | string   | nombre        |
| Apellido       | string   | apellido      |
| Direccion      | string?  | direccion     |
| Correo         | string   | correo (UNIQUE) |
| Area           | string?  | area          |
| Telefono       | string?  | telefono      |
| Usuario        | string   | usuario (UNIQUE) |
| Contrasena     | string   | contrasena    |
| Url            | string?  | url           |
| Turno          | string?  | turno         |

Also create a lightweight `RolModel` class for the `rol` table (id, nombre, descripcion) and an `EmpleadoRolModel` for the junction table.

---

## Step 2: Create `EmpleadoService.cs` in `Services/`

A static class with Npgsql-based methods:

- `GetAllEmpleados()` → `List<EmpleadoModel>`
- `GetEmpleadoByCi(string ci)` → `EmpleadoModel?`
- `InsertEmpleado(EmpleadoModel emp)` → void
- `UpdateEmpleado(EmpleadoModel emp)` → void
- `DeleteEmpleado(string ci)` → void
- `SearchEmpleados(string term)` → `List<EmpleadoModel>` (search by nombre, apellido, ci, usuario)
- `GetAllRoles()` → `List<RolModel>`
- `GetRolesByEmpleado(string ci)` → `List<EmpleadoRolModel>`
- `AssignRoleToEmpleado(string ci, int rolId)` → void
- `RemoveRoleFromEmpleado(string ci, int rolId)` → void

Uses `DatabaseConnection.connectionString` (currently private — will need to make it accessible or add a public static method).

---

## Step 3: Implement `EmpleadosUC.xaml`

Layout structure:

```
Grid (root)
├── Grid.RowDefinitions (2 rows: Auto for toolbar, * for content)
├── Toolbar StackPanel (Row 0)
│   ├── Search Box (TextBox + Search Button)
│   ├── Add New Employee Button
│   └── (optional) Refresh Button
├── Content Grid (Row 1, 2 columns)
│   ├── Left Column (2*) — DataGrid listing employees
│   │   └── DataGrid with columns: CI, Nombre, Apellido, Correo, Área, Turno, Usuario
│   └── Right Column (1*) — Detail/Form panel
│       ├── Form fields (stacked) for all employee properties
│       ├── Role assignment section (ListBox/ComboBox for roles)
│       ├── Save / Update / Cancel buttons
│       └── Delete button
```

Use `DynamicResource` references for theme compatibility (FOscuro1, POscuro2, LOscuro2, NavTextColor, AcentoBrush, etc.).

---

## Step 4: Implement `EmpleadosUC.xaml.cs`

Code-behind logic:

1. **Constructor** — call `LoadEmpleados()` and `LoadRoles()`
2. **LoadEmpleados()** — populate DataGrid from `EmpleadoService.GetAllEmpleados()`
3. **LoadRoles()** — populate role list for assignment
4. **DataGrid SelectionChanged** — populate form fields with selected employee data
5. **Search button / TextChanged** — filter employees via `SearchEmpleados()`
6. **Add New button** — clear form for new entry
7. **Save button** — validate fields, then `InsertEmpleado()` or `UpdateEmpleado()` based on context
8. **Delete button** — confirm dialog, then `DeleteEmpleado()`
9. **Role assignment** — checkboxes or add/remove buttons for roles

---

## Step 5: Add "Empleados" Nav Button in `Dashboard.xaml`

Insert a new button in the sidebar's PRINCIPAL section, following the existing pattern (e.g., after the "Clientes" button). Use a unique icon (e.g., a person silhouette via Canvas paths).

```xml
<Button Style="{StaticResource NavItemStyle}" Click="NavEmpleados_Click">
    <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
        <Canvas Width="20" Height="20" Margin="0,0,12,0">
            <!-- Person icon -->
            <Ellipse Canvas.Left="5" Canvas.Top="1" Width="10" Height="10"
                     Fill="Transparent" Stroke="{DynamicResource NavTextColor}" StrokeThickness="1.5"/>
            <Path Data="M 2,19 Q 2,12 10,12 Q 18,12 18,19"
                  Fill="Transparent" Stroke="{DynamicResource NavTextColor}" StrokeThickness="1.5"/>
        </Canvas>
        <TextBlock Text="Empleados" Foreground="{DynamicResource NavTextColor}"
                   FontSize="15" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

---

## Step 6: Wire Navigation in `Dashboard.xaml.cs`

Add the `NavEmpleados_Click` event handler:

```csharp
private void NavEmpleados_Click(object sender, RoutedEventArgs e)
{
    Contenido.Content = new UserControls.EmpleadosUC();
}
```

Also add the `using ProyectoIntegradorNet10.UserControls;` import.

---

## Step 7: Register Namespace (if needed)

The `Dashboard.xaml` already has `xmlns:local="clr-namespace:ProyectoIntegradorNet10.Windows"`. Since we're loading the UserControl from code-behind, no additional XAML namespace is required.

---

## Database Schema Reference (for implementation)

**`empleado` table:**
- `ci` VARCHAR PK — employee ID (CI number)
- `nombre` VARCHAR NOT NULL
- `apellido` VARCHAR NOT NULL
- `direccion` TEXT nullable
- `correo` VARCHAR NOT NULL UNIQUE
- `area` VARCHAR nullable
- `telefono` VARCHAR nullable
- `usuario` VARCHAR NOT NULL UNIQUE
- `contrasena` VARCHAR NOT NULL
- `url` TEXT nullable
- `turno` VARCHAR nullable

**`rol` table:**
- `id` INTEGER PK
- `nombre` VARCHAR NOT NULL
- `descripcion` TEXT nullable

**`empleado_rol` junction table:**
- `empleado_ci` VARCHAR FK → empleado(ci)
- `rol_id` INTEGER FK → rol(id)
- `estado` VARCHAR nullable
- `fecha_hora_asigacion` TIMESTAMP nullable
- `fecha_hora_fin` TIMESTAMP nullable

---

## Files to Modify

| File | Action |
|------|--------|
| `Models/EmpleadoModel.cs` | **Create** |
| `Services/EmpleadoService.cs` | **Create** |
| `Services/DatabaseConnection.cs` | **Modify** (make connectionString accessible) |
| `UserControls/EmpleadosUC.xaml` | **Modify** (implement UI) |
| `UserControls/EmpleadosUC.xaml.cs` | **Modify** (implement logic) |
| `Windows/Dashboard.xaml` | **Modify** (add nav button) |
| `Windows/Dashboard.xaml.cs` | **Modify** (add click handler) |
