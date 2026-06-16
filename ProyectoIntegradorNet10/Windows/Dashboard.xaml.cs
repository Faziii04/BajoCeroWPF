using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ProyectoIntegradorNet10.Services;
using ProyectoIntegradorNet10.UserControls;
namespace ProyectoIntegradorNet10.Windows
{
    public partial class Dashboard : Window
    {
        public string UsuarioNombre { get; set; } = "usuario";
        public string UsuarioRol { get; set; } = "operario";
        public string UsuarioEmail { get; set; } = string.Empty;
        public string UsuarioUrl { get; set; } = string.Empty;

        /// <summary>
        /// The CI of the logged-in employee, used to load permissions.
        /// </summary>
        public string EmpleadoCi { get; set; } = string.Empty;

        /// <summary>
        /// Set of permission names the current user has (e.g. "VerProductos", "VerInsumos").
        /// </summary>
        public HashSet<string> Permisos { get; set; } = new();

        /// <summary>
        /// Tracks the currently selected nav button for the active visual state.
        /// </summary>
        private Button? _activeNavButton;

        /// <summary>
        /// Maps nav buttons to their display names for the top bar title.
        /// </summary>
        private readonly Dictionary<string, string> _navTitles = new()
        {
            { "navProductos",       "Productos" },
            { "navProduccion",      "Producción" },
            { "navInsumos",         "Insumos" },
            { "navProveedores",     "Proveedores" },
            { "navOrdenesCompra",   "Órdenes de Compra" },
            { "navInventario",      "Inventario" },
            { "navDistribucion",    "Distribución" },
            { "navClientes",        "Clientes" },
            { "navPrestamos",       "Préstamos" },
            { "navEmpleados",       "Empleados" },
            { "navVentasPagos",     "Ventas y Pagos" },
            { "navFacturacion",     "Facturación" },
            { "navReportes",        "Reportes" },
            { "navRolesPermisos",   "Roles y Permisos" },
            { "navVehiculos",       "Vehículos" },
            { "navDepositos",       "Depósitos" },
        };

        public Dashboard()
        {
            InitializeComponent();
            AplicarModo();
            UpdateClock();
        }

        /// <summary>
        /// Updates the time display every 30 seconds.
        /// </summary>
        private void UpdateClock()
        {
            var now = DateTime.Now;
            string dayName = now.ToString("dddd", new System.Globalization.CultureInfo("es-BO"));
            txtHora.Text = $"{dayName}, {now:dd/MM/yyyy} · {now:HH:mm}";

            // Re-schedule
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            timer.Tick += (s, e) => { UpdateClock(); timer.Stop(); };
            timer.Start();
        }

        /// <summary>
        /// Navigates to a UserControl with a fade-in transition and updates the title.
        /// </summary>
        private async void NavigateTo(UserControl content, string title)
        {
            // Fade out current content
            if (Contenido.Content != null)
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
                fadeOut.Completed += (s, e) =>
                {
                    Contenido.Content = content;
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                    Contenido.BeginAnimation(OpacityProperty, fadeIn);
                };
                Contenido.BeginAnimation(OpacityProperty, fadeOut);
            }
            else
            {
                Contenido.Content = content;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                Contenido.BeginAnimation(OpacityProperty, fadeIn);
            }

            txtTituloPagina.Text = title;
        }

        /// <summary>
        /// Call this after setting Permisos to show/hide nav buttons.
        /// </summary>
        public void AplicarPermisos()
        {
            var navMap = new Dictionary<string, string>
            {
                { "navProductos",       "VerProductos" },
                { "navProduccion",      "VerProduccion" },
                { "navInsumos",         "VerInsumos" },
                { "navProveedores",     "VerProveedores" },
                { "navOrdenesCompra",   "VerOrdenesCompra" },
                { "navInventario",      "VerInventario" },
                { "navDistribucion",    "VerDistribucion" },
                { "navClientes",        "VerClientes" },
                { "navPrestamos",       "VerPrestamos" },
                { "navEmpleados",       "VerEmpleados" },
                { "navVentasPagos",     "VerVentasPagos" },
                { "navFacturacion",     "VerFacturacion" },
                { "navReportes",        "VerReportes" },
                { "navRolesPermisos",   "VerRolesPermisos" },
                { "navVehiculos",       "VerVehiculos" },
                { "navDepositos",       "VerDepositos" },
            };

            foreach (var kvp in navMap)
            {
                var btn = this.FindName(kvp.Key) as UIElement;
                if (btn != null)
                {
                    btn.Visibility = Permisos.Contains(kvp.Value)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }

        public void AplicarModo()
        {
            if (GlobalVars.ModoOscuro)
            {
                imgFondoNoche.Visibility = Visibility.Visible;
                imgFondoDia.Visibility = Visibility.Collapsed;
                txtModoIcon.Text = "☀";
                txtModoTexto.Text = "Modo claro";
            }
            else
            {
                imgFondoDia.Visibility = Visibility.Visible;
                imgFondoNoche.Visibility = Visibility.Collapsed;
                txtModoIcon.Text = "🌙";
                txtModoTexto.Text = "Modo oscuro";
            }

            if (!string.IsNullOrEmpty(UsuarioNombre))
            {
                txtNombreUsuario.Text = UsuarioNombre;
                txtInicialUsuario.Text = UsuarioNombre[0].ToString().ToUpper();
            }
            txtRolUsuario.Text = UsuarioRol;
            txtEmailUsuario.Text = UsuarioEmail;

            if (!string.IsNullOrEmpty(UsuarioUrl))
            {
                imgUsuario.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(UsuarioUrl));
                imgUsuario.Visibility = Visibility.Visible;
                txtInicialUsuario.Visibility = Visibility.Collapsed;
            }
            else
            {
                imgUsuario.Visibility = Visibility.Collapsed;
                txtInicialUsuario.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Highlights the given button as the active nav tab.
        /// Sets the left accent bar width and toggles the active visual state.
        /// </summary>
        private void SetActiveNav(Button button)
        {
            // Reset previous active button
            if (_activeNavButton != null && _activeNavButton != button)
            {
                _activeNavButton.ClearValue(BackgroundProperty);
                _activeNavButton.ClearValue(FontWeightProperty);
                _activeNavButton.FontWeight = FontWeights.SemiBold;

                // Reset ActiveBar width on previous button
                ResetActiveBar(_activeNavButton);
            }

            // Set new active button
            _activeNavButton = button;
            _activeNavButton.Background = (Brush)FindResource("GridRowSelectedBrush");
            _activeNavButton.FontWeight = FontWeights.Bold;

            // Set ActiveBar width on the new active button
            SetActiveBarWidth(button, 3);
        }

        /// <summary>
        /// Sets the ActiveBar width inside the nav button template.
        /// </summary>
        private void SetActiveBarWidth(Button button, double width)
        {
            if (button.Template.FindName("ActiveBar", button) is FrameworkElement bar)
            {
                bar.Width = width;
            }
        }

        /// <summary>
        /// Resets the ActiveBar width inside a nav button template.
        /// </summary>
        private void ResetActiveBar(Button button)
        {
            if (button.Template.FindName("ActiveBar", button) is FrameworkElement bar)
            {
                bar.Width = 0;
            }
        }

        private void btnModo_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.SwitchTheme();
            AplicarModo();

            // Re-apply active nav so theme-aware brushes update
            if (_activeNavButton != null)
                SetActiveNav(_activeNavButton);
        }

        // --- Navigation ---

        private void NavProductos_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new ProductosUC(), _navTitles["navProductos"]);
            SetActiveNav(navProductos);
        }

        private void NavProduccion_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new ProduccionUC(), _navTitles["navProduccion"]);
            SetActiveNav(navProduccion);
        }

        private void NavInsumos_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new InsumosUC(), _navTitles["navInsumos"]);
            SetActiveNav(navInsumos);
        }

        private void NavProveedores_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new ProveedoresUC(), _navTitles["navProveedores"]);
            SetActiveNav(navProveedores);
        }

        private void NavOrdenesCompra_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new OrdenesCompraUC(), _navTitles["navOrdenesCompra"]);
            SetActiveNav(navOrdenesCompra);
        }

        private void NavInventario_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new InventarioUC(), _navTitles["navInventario"]);
            SetActiveNav(navInventario);
        }

        private void NavDistribucion_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new DistribucionUC(), _navTitles["navDistribucion"]);
            SetActiveNav(navDistribucion);
        }

        private void NavClientes_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new ClientesUC(), _navTitles["navClientes"]);
            SetActiveNav(navClientes);
        }

        private void NavPrestamos_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new PrestamosUC(), _navTitles["navPrestamos"]);
            SetActiveNav(navPrestamos);
        }

        private void NavEmpleados_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new EmpleadosUC(), _navTitles["navEmpleados"]);
            SetActiveNav(navEmpleados);
        }

        private void NavVentasPagos_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new VentasPagosUC(), _navTitles["navVentasPagos"]);
            SetActiveNav(navVentasPagos);
        }

        private void NavFacturacion_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new FacturacionUC(), _navTitles["navFacturacion"]);
            SetActiveNav(navFacturacion);
        }

        private void NavReportes_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new ReportesUC(), _navTitles["navReportes"]);
            SetActiveNav(navReportes);
        }

        private void NavRolesPermisos_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new RolesPermisosUC(), _navTitles["navRolesPermisos"]);
            SetActiveNav(navRolesPermisos);
        }

        private void NavVehiculos_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new VehiculosUC(), _navTitles["navVehiculos"]);
            SetActiveNav(navVehiculos);
        }

        private void NavDepositos_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new DepositosUC(), _navTitles["navDepositos"]);
            SetActiveNav(navDepositos);
        }

        // --- Window control buttons ---

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // --- Logout ---

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }

        // --- Window dragging ---

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
