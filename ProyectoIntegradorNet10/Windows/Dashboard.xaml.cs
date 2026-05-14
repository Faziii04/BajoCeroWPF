using System;
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

        public Dashboard()
        {
            InitializeComponent();
            AplicarModo();
        }

        private void AplicarModo()
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
