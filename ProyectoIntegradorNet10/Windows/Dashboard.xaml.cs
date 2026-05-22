using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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

        private void btnModo_Click(object sender, RoutedEventArgs e)
        {
            GlobalVars.SwitchTheme();
            AplicarModo();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Dashboard button clicked!");
        }

        // --- Navigation ---

        private void NavProductos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ProductosUC();
        }

        private void NavProduccion_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ProduccionUC();
        }

        private void NavInsumos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new InsumosUC();
        }

        private void NavProveedores_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ProveedoresUC();
        }

        private void NavOrdenesCompra_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new OrdenesCompraUC();
        }

        private void NavInventario_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new InventarioUC();
        }

        private void NavDistribucion_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new DistribucionUC();
        }

        private void NavClientes_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ClientesUC();
        }

        private void NavPrestamos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new PrestamosUC();
        }

        private void NavEmpleados_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new EmpleadosUC();
        }

        private void NavVentasPagos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new VentasPagosUC();
        }

        private void NavFacturacion_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new FacturacionUC();
        }

        private void NavReportes_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new ReportesUC();
        }

        private void NavRolesPermisos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new RolesPermisosUC();
        }

        private void NavVehiculos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new VehiculosUC();
        }

        private void NavDepositos_Click(object sender, RoutedEventArgs e)
        {
            Contenido.Content = new DepositosUC();
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
