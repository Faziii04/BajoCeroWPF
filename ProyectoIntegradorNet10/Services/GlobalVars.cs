using System;
using System.Windows;

namespace ProyectoIntegradorNet10.Services
{
    public static class GlobalVars
    {
        public static bool ModoOscuro { get; set; } = true; // Default: dark mode

        // Light theme dictionary (lazy-loaded)
        private static ResourceDictionary _lightThemeDict;
        public static ResourceDictionary LightThemeDict
        {
            get
            {
                if (_lightThemeDict == null)
                {
                    _lightThemeDict = new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/Themes/LightTheme.xaml")
                    };
                }
                return _lightThemeDict;
            }
        }

        // Dark theme dictionary (lazy-loaded)
        private static ResourceDictionary _darkThemeDict;
        public static ResourceDictionary DarkThemeDict
        {
            get
            {
                if (_darkThemeDict == null)
                {
                    _darkThemeDict = new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                    };
                }
                return _darkThemeDict;
            }
        }

        /// <summary>
        /// Toggles the theme between light and dark by swapping the
        /// merged dictionary at the Application level.
        /// </summary>
        public static void SwitchTheme()
        {
            ModoOscuro = !ModoOscuro;

            var appResources = Application.Current.Resources;
            appResources.MergedDictionaries.Clear();

            if (ModoOscuro)
                appResources.MergedDictionaries.Add(DarkThemeDict);
            else
                appResources.MergedDictionaries.Add(LightThemeDict);
        }

        /// <summary>
        /// Applies the current theme (called once at startup).
        /// </summary>
        public static void ApplyInitialTheme()
        {
            var appResources = Application.Current.Resources;
            appResources.MergedDictionaries.Clear();

            if (ModoOscuro)
                appResources.MergedDictionaries.Add(DarkThemeDict);
            else
                appResources.MergedDictionaries.Add(LightThemeDict);
        }
    }
}
