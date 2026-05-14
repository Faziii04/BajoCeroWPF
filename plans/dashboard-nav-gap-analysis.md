# Dashboard Navigation Gap Analysis

> **Date:** 2026-05-14
> **Scope:** Compare current sidebar navigation in `Dashboard.xaml` against the database schema to identify missing admin panel sections.

---

## ✅ Currently Present (8 nav items)

| Nav Item | DB Entity Covered | Has Click Handler? |
|----------|-------------------|:------------------:|
| Dashboard | — (home view) | ✅ |
| Producción | `produccion`, `produccion_producto`, `insumo_produccion` | ❌ |
| Insumos | `insumos` | ❌ |
| Inventario | `producto_deposito`, `deposito` | ❌ |
| Distribución | `repartidor`, `repartidor_vehiculo` | ❌ |
| Clientes | `cliente` | ❌ |
| Empleados | `empleado` | ✅ |
| Ventas y pagos | `venta`, `pago`, `pago_venta`, `venta_detalles` | ❌ |
| Reportes | — (analytics section) | ❌ |

---

## ❌ Missing Nav Items (DB entities with no nav entry)

### 🔴 HIGH Priority

| Missing Entity | DB Table(s) | Why an Admin Needs It |
|:--------------|:------------|:----------------------|
| **Productos** | `producto` | Core entity referenced by production, inventory, sales, and loans. No way to manage products currently. |
| **Proveedores** | `proveedor` | Suppliers are linked to purchase orders; admin needs CRUD. |
| **Órdenes de Compra** | `orden_compra`, `detalles_orden` | The entire procurement cycle (suppliers → purchase orders → supply inventory) is missing. |

### 🟡 MEDIUM Priority

| Missing Entity | DB Table(s) | Why an Admin Needs It |
|:--------------|:------------|:----------------------|
| **Préstamos** | `prestamo`, `prestamo_detalle` | Distinct business process — loaning products to clients with replacement value. Not covered by "Ventas". |
| **Facturación** | `factura` | Invoices have their own table (subtotal, total, discount, NIT). Could be a sub-item under "Ventas y pagos" or standalone. |
| **Roles y Permisos** | `rol`, `permiso`, `empleado_rol`, `rol_permisos` | Admin panel needs role/perm management. "Empleados" exists but without role assignment UI. |
| **Vehículos** | `vehiculo` | Vehicles managed separately from repartidores (SOAT, mileage, model). Could be under "Distribución" or standalone. |

### 🟢 LOW Priority

| Missing Entity | DB Table(s) | Why an Admin Needs It |
|:--------------|:------------|:----------------------|
| **Depósitos** | `deposito` | Warehouses/deposits. "Inventario" covers product-deposit relationships, but managing deposit locations themselves is missing. |

---

## Suggested Section Restructure

```
PRINCIPAL
├── Dashboard
├── Productos              ← MISSING
├── Producción
├── Insumos
├── Inventario
├── Proveedores            ← MISSING
├── Órdenes de Compra      ← MISSING
├── Distribución
├── Clientes
├── Empleados
├── Ventas y pagos
├── Préstamos              ← MISSING
├── Facturación            ← MISSING

ANALISIS
├── Reportes

ADMINISTRACIÓN             ← NEW SECTION
├── Roles y Permisos       ← MISSING
├── Vehículos              ← MISSING
├── Depósitos              ← MISSING
```

---

## Additional Notes

- Most nav buttons in the sidebar **lack click handlers** — only "Dashboard", "Empleados", and "Cerrar sesión" have wired-up events in `Dashboard.xaml.cs`. The rest are visual-only placeholders.
- Only one `UserControl` exists so far: `EmpleadosUC`. All other sections need their own views created.
