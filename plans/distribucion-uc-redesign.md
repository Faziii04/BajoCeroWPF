# DistribucionUC Redesign Plan

## Overview
Repurpose the current empty `DistribucionUC` (which had stub fields for Repartidor/Vehiculo/Ruta) into a **distribution management dashboard** that shows sales needing delivery, filtered by delivery estado.

## Data Model
The UC will use the existing `VentaModel` and `VentasService`. Each `VentaModel` has:
- `Id`, `Estado` (Pedido / En ruta / Incidencia / Completado)
- `ClienteNombre`, `ClienteCi`
- `Delivery` (bool — only delivery-required sales should appear)
- `RepartidorId`, `RepartidorNombre` (nullable — assigned when going to "En ruta")
- `Fecha`, `Hora`, `Total`, `Tipo`
- `Detalles` (product list)

Plus existing `RepartidorService` + `VehiculoService` for assignment.

## Layout
Follow the existing split-panel pattern already in the XAML:

### Left panel (2* width) — Sales grid
- **Toolbar**: Icon badge + "Distribución" title, count badge, estado filter chips, search box
- **Estado filter chips**: Pedido (default), En ruta, Incidencia, Completado — clickable toggle pills
- **DataGrid columns**:
  - ID
  - Cliente (icon + name)
  - Fecha
  - Total
  - Estado (with colored badge)
  - Delivery (Sí/No icon)
- Empty state with icon when no results

### Right panel (1.2* width) — Action form
- **Venta info header**: Shows selected venta ID, cliente, total
- **Estado selector**: ComboBox or chips to change estado
- **Repartidor assignment**: ComboBox with list of active repartidores (from RepartidorService.GetActivos) + Assigned repartidor display
- **Vehículo**: Display or vehicle assignment info
- **Action buttons**:
  - "Asignar Repartidor" / "Cambiar Estado" primary button
  - "Marcar Completado" when both delivered criteria are met
- When no venta selected: show placeholder text

## Code-Behind Logic

### VentasService — Add method
Add `GetVentasByEstado(string estado)` to VentasService:
```sql
SELECT ... FROM venta v
LEFT JOIN cliente c ON c.ci = v.cliente_ci
WHERE v.delivery = true AND v.estado = @estado
ORDER BY v.id DESC
```

### DistribucionUC.xaml.cs — Main logic
1. **LoadVentas(string estado)**: Load ventas filtered by estado, populate DataGrid
2. **Estado chip click**: Toggle active filter, reload ventas
3. **dgVentas_SelectionChanged**: Load selected venta details into right panel
4. **Asignar Repartidor**: Open a small popup or inline combobox to assign repartidor + update `venta.repartidor_id` via VentasService.UpdateVenta
5. **Cambiar Estado**: Update `venta.estado` via VentasService.UpdateVenta
6. **Search**: Filter ventas by ID or cliente name within the current estado

### Estado color mapping
- Pedido → Blue/Neutral
- En ruta → Yellow/Warning
- Incidencia → Red/Danger
- Completado → Green/Success

Theme colors to use: existing brush resources (AcentoBrush, SoatVigenteBrush, etc.)

## Files to modify

| File | Changes |
|------|---------|
| `Services/VentasService.cs` | Add `GetVentasByEstado(string estado)` method |
| `UserControls/DistribucionUC.xaml` | Replace with redesigned layout |
| `UserControls/DistribucionUC.xaml.cs` | Replace with full logic |

## States

### Empty state
- No ventas match the filter → 📦 "No hay ventas en estado [Pedido]"
- No venta selected in right panel → "Seleccione una venta para gestionar su distribución"

### Loading state
- Disable buttons while saving/assigning
- Show "Guardando..." on action button during save

### Error state
- MessageBox on service errors
