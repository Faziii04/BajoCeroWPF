
using System;
using System.Windows;
using ProyectoIntegradorNet10.Services;
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
    }
}

