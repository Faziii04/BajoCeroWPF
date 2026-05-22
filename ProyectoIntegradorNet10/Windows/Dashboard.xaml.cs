using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        public Dashboard()
        {
            InitializeComponent();
            AplicarModo();
        }

        /// <summary>
        /// Call this after setting Permisos to show/hide nav buttons.
        /// </summary>
        public void AplicarPermisos()
        {
            // Map each nav button name → its permission name
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
                // Find the button by name in the visual tree
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
            }
            else
            {
                imgFondoDia.Visibility = Visibility.Visible;
                imgFondoNoche.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(UsuarioNombre))
            {
                txtNombreUsuario.Text = UsuarioNombre;
                txtInicialUsuario.Text = UsuarioNombre[0].ToString().ToUpper();
            }
            txtRolUsuario.Text = UsuarioRol;

            // Show email
            txtEmailUsuario.Text = UsuarioEmail;

            // Show photo if URL is provided, otherwise hide the image
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
        /// Highlights the given button as the active nav tab and clears the previous one.
        /// </summary>
        private void SetActiveNav(Button button)
        {
            // Reset previous active button
            if (_activeNavButton != null && _activeNavButton != button)
            {
                _activeNavButton.ClearValue(BackgroundProperty);
                _activeNavButton.ClearValue(BorderBrushProperty);
                _activeNavButton.ClearValue(BorderThicknessProperty);
            }

            // Set new active button using theme-aware resources
            _activeNavButton = button;
            _activeNavButton.Background = (Brush)FindResource("GridRowSelectedBrush");
            _activeNavButton.BorderBrush = (Brush)FindResource("NavTextColor");
            _activeNavButton.BorderThickness = new Thickness(3, 0, 0, 0);
        }

        private void btnModo_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.SwitchTheme();
            AplicarModo();
        }

        // --- Navigation ---

        private void NavProductos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ProductosUC();
            SetActiveNav(navProductos);
        }

        private void NavProduccion_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ProduccionUC();
            SetActiveNav(navProduccion);
        }

        private void NavInsumos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new InsumosUC();
            SetActiveNav(navInsumos);
        }

        private void NavProveedores_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ProveedoresUC();
            SetActiveNav(navProveedores);
        }

        private void NavOrdenesCompra_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new OrdenesCompraUC();
            SetActiveNav(navOrdenesCompra);
        }

        private void NavInventario_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new InventarioUC();
            SetActiveNav(navInventario);
        }

        private void NavDistribucion_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new DistribucionUC();
            SetActiveNav(navDistribucion);
        }

        private void NavClientes_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ClientesUC();
            SetActiveNav(navClientes);
        }

        private void NavPrestamos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new PrestamosUC();
            SetActiveNav(navPrestamos);
        }

        private void NavEmpleados_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new EmpleadosUC();
            SetActiveNav(navEmpleados);
        }

        private void NavVentasPagos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new VentasPagosUC();
            SetActiveNav(navVentasPagos);
        }

        private void NavFacturacion_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new FacturacionUC();
            SetActiveNav(navFacturacion);
        }

        private void NavReportes_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ReportesUC();
            SetActiveNav(navReportes);
        }

        private void NavRolesPermisos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new RolesPermisosUC();
            SetActiveNav(navRolesPermisos);
        }

        private void NavVehiculos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new VehiculosUC();
            SetActiveNav(navVehiculos);
        }

        private void NavDepositos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new DepositosUC();
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

        // --- Logout: go back to login ---

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }

        // --- Window dragging (since WindowStyle="None") ---

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
