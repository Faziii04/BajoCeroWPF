# Prompt para Claude — Generar Documentación del Proyecto BajoCero

Copia y pega el siguiente bloque completo en Claude para generar ambos documentos.

---

```
# Contexto del Proyecto

## Nombre del Proyecto
**BajoCero — Sistema de Gestión Integral** (AguaPotable Pro)

## Tipo de Aplicación
Aplicación de escritorio WPF (.NET 10) con arquitectura de capas, orientada a la gestión integral de una empresa embotelladora/distribuidora de agua.

## Tecnologías y Stack
- **Lenguaje/Framework:** C# 13, .NET 10.0-windows, WPF
- **Base de datos:** PostgreSQL 15 hospedada en Supabase (conexión mediante Npgsql con pool de conexiones)
- **ORM:** Ninguno (ADO.NET directo con NpgsqlDataReader y commands parametrizados)
- **Storage de imágenes:** Supabase S3-compatible (AWS SDK S3) — buckets: products, employees_pfp, clients_pfp, families, pagos
- **Geocodificación:** LocationIQ API (búsqueda de direcciones → lat/lon)
- **Mapas:** GMap.NET WPF (visualización de rutas de distribución)
- **Reportes/Exportación:** ClosedXML (exportación a Excel)
- **Temas:** Diccionarios de recursos WPF dinámicos (DarkTheme.xaml, LightTheme.xaml, SharedStyles.xaml)
- **IDE:** Visual Studio 2022 / VS Code

## Arquitectura General
La aplicación sigue una arquitectura simple de 3 capas:

1. **Capa de Presentación (UI):**
   - `MainWindow.xaml` — Ventana de login con fondo dinámico (día/noche), campos de usuario/contraseña, botón de inicio de sesión y "Modo prueba".
   - `Dashboard.xaml` — Ventana principal maximizada con sidebar de navegación (260px) + top bar + área de contenido. Los módulos se cargan como UserControls dentro de un ContentControl con animación fade-in/out.
   - **16 UserControls** (módulos de gestión, ver lista completa abajo)
   - **17 PopWindows** (ventanas modales para crear/editar registros)

2. **Capa de Negocio (Servicios):**
   - `DatabaseConnection.cs` — Singleton de NpgsqlDataSource con lazy loading, connection string a Supabase
   - **18 clases Service** estáticas con métodos CRUD asíncronos
   - `GlobalVars.cs` — Variables globales (tema oscuro/claro, cambio de tema en tiempo real)
   - `S3Helper.cs` — Subida/borrado de imágenes a Supabase Storage
   - `LocationIQHelper.cs` — Geocodificación de direcciones

3. **Capa de Datos (Modelos):**
   - **18 clases Model** (ver lista completa abajo)
   - **8 clases DTO/Reporte** en `ReportesService.cs`

## Sistema de Autenticación y Permisos
- Login contra tabla `empleado` con usuario/contraseña en texto plano (sin hash)
- 3 intentos máximos antes de cerrar la aplicación
- "Modo prueba" otorga todos los permisos automáticamente
- Sistema de roles y permisos basado en 4 tablas: `rol` → `permiso` → `rol_permisos` → `empleado_rol`
- Los permisos se cargan al iniciar sesión como `HashSet<string>` y controlan la visibilidad de los botones de navegación
- 16 permisos disponibles: VerProductos, VerProduccion, VerInsumos, VerProveedores, VerOrdenesCompra, VerInventario, VerDistribucion, VerClientes, VerPrestamos, VerEmpleados, VerVentasPagos, VerFacturacion, VerReportes, VerRolesPermisos, VerVehiculos, VerDepositos

## Módulos del Sistema (16 UserControls + 2 adicionales)

### 1. ProductosUC
- Galería de tarjetas de productos con imagen, nombre, precio y stock
- Filtro por familias mediante chips (categorías)
- Búsqueda por nombre
- CRUD vía PWProductos (popup modal con campo de imagen)
- Vista de familias dentro (FamiliaInsideUC / ProductoInsideUC)

### 2. ProduccionUC
- Gestión de producción (detalles no explorados a fondo, pero relacionado con fabricación de productos)

### 3. InsumosUC
- CRUD de insumos (materias primas) con nombre, descripción, unidad de medida, precio unitario, cantidad en stock
- PWInsumos para crear/editar

### 4. ProveedoresUC
- CRUD de proveedores con nombre, dirección, teléfono, descripción
- PWProveedores para crear/editar

### 5. OrdenesCompraUC
- Órdenes de compra a proveedores con fecha de pedido, hora, estado, monto, proveedor
- Fechas de llegada estimadas
- Detalle de insumos por orden (DetalleOrdenModel)
- PWOrdenesCompra

### 6. InventarioUC
- Stock de productos por depósito (tabla producto_deposito)
- Vista con producto, depósito, cantidad
- PWInventario

### 7. DistribucionUC
- Gestión de rutas de distribución con mapa GMap.NET
- Asignación de repartidores a ventas con delivery
- Seguimiento de estado: Pedido → En ruta → Incidencia → Completado
- Asignación de repartidor a vehículo
- PWDistribucion, PWAsignarRepartidor
- Ubicación de clientes en mapa con coordenadas (latitud/longitud de LocationIQ)

### 8. ClientesUC
- CRUD de clientes con CI, nombre, apellido, dirección, NIT, teléfono
- Foto de perfil (subida a S3)
- Geolocalización del cliente (latitud/longitud)
- Búsqueda con debounce
- PWClientes

### 9. PrestamosUC
- Sistema de préstamos de productos a clientes
- Detalle de productos prestados con cantidad y valor de reposición
- PWPrestamo

### 10. EmpleadosUC
- CRUD de empleados con CI, nombre, apellido, dirección, correo, área, teléfono, usuario, contraseña, turno
- Foto de perfil (subida a S3)
- Asignación de roles a empleados (Activo/Inactivo con fechas)
- Búsqueda
- PWEmployees

### 11. VentasPagosUC (módulo principal de ventas)
- DataGrid de ventas con filtros avanzados:
  - Por cliente (editable ComboBox con filtro)
  - Rango de fechas
  - Rango de fecha de entrega
  - Tipo (Contado / Plan de pago)
  - Estado (Pedido / En ruta / Incidencia / Completado)
  - Delivery (Sí/No)
  - Pagado (Sí/No)
  - Entregado (Sí/No)
- Doble clic para editar venta → abre PWVentas
- Botón "Nuevo" → abre PWVentas

### 12. VentasUC (popup interno de PWVentas)
- Creación/edición de ventas con:
  - Selección de cliente (ComboBox editable con filtro)
  - Tipo: Contado / Plan de pago (con selector de meses)
  - Galería de productos (con filtro por familia)
  - Cantidad y precio
  - Descuento porcentual
  - NIT del cliente (botón para traer automático)
  - Delivery (con fecha/hora de entrega)
  - Estado de entrega
  - Pagado / Entregado (checkboxes)
  - Botón "Marcar Completado" cuando ambos están chequeados
  - Generación automática de pagos mensuales para Plan de pago
  - STOCK VALIDATION: verifica stock suficiente antes de guardar

### 13. PagosUC (popup interno de PWVentas)
- Lista de pagos asociados a una venta
- CRUD de pagos con monto, método (Efectivo/QR/Transferencia), estado (Pagado/Pendiente/Vencido)
- La pestaña aparece dentro del popup PWVentas

### 14. FacturacionUC
- DataGrid de facturas con búsqueda
- Vista de detalle con información completa: ID, venta #, fecha, cliente, nombre, NIT, subtotal, descuento, total
- Lista de productos de la venta
- Lista de pagos de la venta con estado
- Eliminación de facturas

### 15. ReportesUC
- Reportes con filtros de fecha y exportación a Excel (ClosedXML):
  1. Productos más vendidos (con total vendido e ingresos)
  2. Inventario actual por producto
  3. Clientes frecuentes (top 50 por compras)
  4. Empleados por área (con roles)
  5. Ventas por período
  6. Facturación (con filtro de fechas)
  7. Roles y permisos (con conteo de empleados y permisos)
  8. Vehículos (con estado SOAT y repartidor asignado)
  9. Depósitos (con stock total y productos count)

### 16. RolesPermisosUC
- Gestión de roles: CRUD con nombre y descripción
- Gestión de permisos: CRUD con nombre y descripción
- Asignación de permisos a roles (Activo/Inactivo con fechas)
- Tabla pivote rol_permisos
- PWRoles

### 17. VehiculosUC
- CRUD de vehículos con placa, marca, modelo, tipo, kilometraje, SOAT vencimiento
- Indicador visual de estado de SOAT (Vigente/Por vencer/Vencido) con colores (verde/amarillo/rojo)
- Asignación de repartidor a vehículo

### 18. DepositosUC
- CRUD de depósitos/almacenes con nombre, dirección, ubicación
- Gestión de stock por depósito
- PWInventario (producto_deposito)

## Modelos de Datos (18 clases)

1. **EmpleadoModel** — Ci, Nombre, Apellido, Direccion, Correo, Area, Telefono, Usuario, Contrasena, Url, Turno + Iniciales
2. **RolModel** — Id, Nombre, Descripcion
3. **EmpleadoRolModel** — EmpleadoCi, RolId, Estado, FechaHoraAsignacion, FechaHoraFin, RolNombre
4. **PermisoModel** — Id, Permiso, Descripcion
5. **RolPermisoModel** — RolId, PermisoId, Estado, FechaAsignacion, FechaFin, PermisoNombre
6. **ClienteModel** — Ci, Nombre, Apellido, Direccion, Nit, Telefono, Url, Latitud, Longitud + NombreCompleto
7. **ProductoModel** — Id, Nombre, PrecioVenta, Estado, Url, StockTotal + PrecioDisplay, StockDisplay
8. **FamiliaModel** — Id, Nombre, Descripcion, Url, MiembroCount
9. **InsumoModel** — Id, Nombre, Descripcion, UnidadMedida, PrecioUnitario, CantidadStock
10. **ProveedorModel** — Id, Nombre, Direccion, Telefono, Descripcion
11. **OrdenCompraModel** — Id, FechaPedido, HoraPedido, Estado, Monto, ProveedorId, ProveedorNombre, FechaLlegada, HoraLlegada
12. **DetalleOrdenModel** — OrdenId, InsumoId, Cantidad, InsumoNombre, InsumoPrecio
13. **InventarioModel** — ProductoId, DepositoId, Cantidad, ProductoNombre, DepositoNombre
14. **VentaModel** — Id, Fecha, Hora, Tipo, Estado, PorcentajeDescuento, RepartidorId, ClienteCi, Pagado, Entregado, Nit, Delivery, FechaEntrega, HoraEntrega, FechaEntregado, HoraEntregado, ClienteNombre, RepartidorNombre, Detalles, Meses, Total
15. **VentaDetalleModel** — ProductoId, VentaId, Cantidad, PrecioUnitario, ProductoNombre, Subtotal
16. **PagoModel** — PagoId, Fecha, Hora, Monto, Metodo, Estado, VentaId
17. **FacturaModel** — Id, VentaId, Subtotal, Total, Descuento, FechaEmision, NombreCompleto, Nit, DescuentoTipo, VentaTipo, VentaEstado, ClienteNombre, ProductosList, PagosList, TotalPagado
18. **PrestamoModel** — Id, Fecha, Estado, ClienteNombre, ValorTotal
19. **PrestamoDetalleModel** — ClienteCi, ProductoId, PrestamoId, Cantidad, ValorReposicion, ProductoNombre, ProductoPrecio
20. **IncidenciaModel** — Id, Fecha, Hora, Motivo, Resuelto, Notas, VentaId
21. **RepartidorModel** — Id, Estado, Zona, Licencia, EmpleadoCi, EmpleadoNombre
22. **RepartidorVehiculoModel** — RepartidorId, VehiculoPlaca, Estado, FechaHoraAsignacion, FechaHoraFin
23. **VehiculoModel** — Placa, Marca, Modelo, Tipo, Kilometraje, SoatVencimiento, UltimaActualizacion, RepartidorAsignado, TieneRepartidorActivo + SoatEstado, SoatColorBrush

## Base de Datos (PostgreSQL en Supabase)
Esquema con las siguientes tablas principales:
- empleado, cliente, producto, familia, insumo, proveedor
- producto_familia (relación N:M), producto_deposito (stock por depósito)
- venta, venta_detalles, pago, pago_venta (relación N:M)
- factura
- orden_compra, detalle_orden
- prestamo, prestamo_detalle
- incidencia
- repartidor, repartidor_vehiculo
- vehiculo, deposito
- rol, permiso, rol_permisos, empleado_rol

## UI/UX Detalles
- **Tema:** Soporte completo de modo oscuro/claro con cambio en caliente (sin reinicio)
- **Ventana:** Sin bordes (WindowStyle="None"), con arrastre personalizado, botones minimize/maximize/close
- **Sidebar:** 260px con imagen de fondo, brand "BajoCero", avatar de empleado, iniciales si no hay foto, navegación con íconos SVG, secciones (PRINCIPAL / ANÁLISIS / ADMINISTRACIÓN), indicador activo con barra lateral, footer con toggle de tema y logout
- **Top bar:** Título de página, reloj en vivo con formato local (es-BO), controles de ventana
- **Animaciones:** Fade-in/out al navegar entre módulos, efecto de escala al presionar botones
- **DataGrids:** Estilo unificado con filas alternadas, hover, selección, cabeceras estilizadas
- **Scrollbars:** Personalizadas, delgadas (10px), con hover accent color
- **Formularios:** Inputs con borde redondeado (corner radius 8), focus accent color, labels semi-bold
- **Checkboxes:** Personalizados con icono check, accent color
- **Botones:** 4 variantes — ActionButton (accent), DangerButton (rojo), SecondaryButton (neutral), OutlineButton (bordeado)
- **Chips de filtro:** Familias con toggle selection, estilo pill redondeado
- **Tarjetas de producto:** En galería ItemsControl con imagen, nombre, precio, stock

## Servicios Externos
1. **Supabase (PostgreSQL):** aws-1-us-east-2.pooler.supabase.com:5432
2. **Supabase S3 Storage:** Bucket "images" con subdirectorios por tipo
3. **LocationIQ API:** Geocodificación directa e inversa
4. **GMap.NET:** Mapas WPF offline/online

---

# Tarea: Generar la Documentación

## Documento 1: Documentación Completa del Proyecto

Genera un documento extenso y profesional en español que cubra:

### 1. Introducción
- Descripción del sistema "BajoCero" y su propósito
- Alcance del proyecto
- Público objetivo de la documentación

### 2. Arquitectura del Sistema
- Diagrama de arquitectura general (3 capas)
- Patrones utilizados (Singleton para DB, estáticos para servicios)
- Flujo de datos desde la UI hasta la BD
- Seguridad y autenticación

### 3. Stack Tecnológico
- Detalle de cada tecnología: .NET 10, WPF, PostgreSQL, Supabase, AWS S3, LocationIQ, ClosedXML, GMap.NET
- Justificación de cada elección técnica

### 4. Estructura del Proyecto
- Árbol de directorios completo con explicación
- Namespaces y organización del código

### 5. Base de Datos
- Diagrama entidad-relación (en texto/Mermaid)
- Descripción de todas las tablas con columnas, tipos y relaciones
- Esquema SQL completo
- Políticas de conexión y pooling

### 6. Modelos de Datos
- Lista completa de todas las clases Model con propiedades y relaciones
- Mapeo a tablas de BD

### 7. Servicios (Capa de Negocio)
- Descripción de cada clase Service con sus métodos principales
- Patrón de conexión (NpgsqlDataSource)
- Manejo de excepciones y transacciones

### 8. Interfaz de Usuario (Módulo de Escritorio)
- Descripción detallada de cada ventana y control:
  - Login (MainWindow)
  - Dashboard (navegación, sidebar, top bar)
  - Los 16 módulos UserControl + 2 adicionales (ver lista arriba)
  - Las 17 ventanas PopUp
- Temas (claro/oscuro) y SharedStyles
- Sistema de navegación y animaciones
- Control de permisos por rol

### 9. Integraciones Externas
- Supabase Storage (S3): subida/borrado de imágenes
- LocationIQ: geocodificación
- GMap.NET: visualización de mapas

### 10. Funcionalidades Clave
- Sistema de ventas con contado/plan de pagos
- Delivery y distribución con seguimiento de estado
- Control de inventario por depósito
- Facturación electrónica
- Préstamos de productos
- Reportes con exportación a Excel
- Roles y permisos granulares
- Gestión de flota de vehículos con estado SOAT

### 11. Consideraciones Técnicas
- Manejo de errores
- Performance (pooling de conexiones, virtualización)
- Multi-theming
- Dependencias y paquetes NuGet

### 12. Glosario de Términos

---

## Documento 2: Manual de Usuario (Módulo de Escritorio)

Genera un manual de usuario completo en español, orientado a operadores y administradores, que incluya:

### 1. Introducción
- ¿Qué es BajoCero?
- Requisitos del sistema
- Cómo iniciar la aplicación

### 2. Primeros Pasos
- **Inicio de sesión:** Pantalla de login, campos de usuario/contraseña
- **Modo prueba:** Cómo acceder sin credenciales
- **Recuperación de contraseña:** (si aplica)
- **Cambio de tema:** Oscuro/claro

### 3. El Dashboard (Panel Principal)
- Layout general: sidebar, top bar, área de contenido
- Barra lateral: navegación por módulos
- Información del usuario (avatar, nombre, rol, email)
- Reloj y fecha en vivo
- Cerrar sesión
- Controles de ventana (minimizar, maximizar, cerrar)

### 4. Guía por Módulos (cada uno con capturas descritas y pasos)

Para cada módulo, incluir:
- Propósito del módulo
- Cómo acceder
- Descripción de la interfaz
- Acciones disponibles (crear, editar, eliminar, buscar, filtrar)
- Campos del formulario
- Ejemplos de uso

**Módulos a cubrir:**

1. **Productos** — Gestión de productos, familias, galería de tarjetas, filtro por familias, búsqueda
2. **Producción** — Gestión de procesos productivos
3. **Insumos** — Materias primas, stock, precios
4. **Proveedores** — Registro de proveedores
5. **Órdenes de Compra** — Pedidos a proveedores, seguimiento de llegada
6. **Inventario** — Stock por depósito
7. **Distribución** — Rutas, mapa, asignación de repartidores, seguimiento de entregas
8. **Clientes** — Registro, NIT, geolocalización
9. **Préstamos** — Préstamo de productos a clientes
10. **Empleados** — Gestión de personal, roles, fotos
11. **Ventas y Pagos** — **Módulo central:** crear ventas (contado/plan pagos), agregar productos, delivery, pagos
12. **Facturación** — Ver y eliminar facturas, detalle con productos y pagos
13. **Reportes** — Generar reportes, filtrar por fechas, exportar a Excel
14. **Roles y Permisos** — Crear roles, asignar permisos, control de acceso
15. **Vehículos** — Flota, SOAT, asignación de repartidores
16. **Depósitos** — Almacenes, capacidad, stock

### 5. Funcionalidades Transversales
- **Búsqueda:** Cómo usar los campos de búsqueda en cada módulo
- **Filtros:** Filtros por familia, fechas, estado, tipo
- **Exportación a Excel:** Desde el módulo de Reportes
- **Cambio de tema:** Cómo y cuándo usarlo

### 6. Solución de Problemas Comunes
- Error de conexión a la base de datos
- Stock insuficiente al crear venta
- Imagen que no se carga
- Permiso denegado al acceder a un módulo
- Problemas con LocationIQ (geocodificación)

### 7. Preguntas Frecuentes (FAQ)
- ¿Cómo crear un plan de pagos?
- ¿Cómo asignar un repartidor?
- ¿Cómo marcar una venta como completada?
- ¿Cómo exportar un reporte?
- ¿Cómo cambiar la foto de un empleado/cliente?
- ¿Qué significa cada estado de SOAT?

### Índice y Navegación
- Incluir tabla de contenidos al inicio
- Numeración de páginas
- Referencias cruzadas entre secciones

---

# Formato de Entrega

Ambos documentos deben generarse en **español (Bolivia)** con:
- Formato Markdown listo para exportar a PDF o DOCX
- Tablas para datos estructurados (campos de modelos, tablas BD, etc.)
- Diagramas Mermaid donde sea útil (ERD, flujo de ventas, arquitectura)
- Sin marcadores de posición (todo debe estar completo y específico al proyecto)
- Estilo profesional y técnico
```
