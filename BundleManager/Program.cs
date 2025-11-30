using PluginSystem;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using Microsoft.Win32;

using BundleManager.TypeConverters;

namespace BundleManager
{

    public static class Program
    {
        public static MainForm fileModeForm;
        public static FileView folderModeForm;

        private const string RegistryKeyPath = @"Software\BurnoutHints\BundleManager";
        private const string RegistryValueName = "PreferredMode"; // 1 = Studio, 0 = Bundle

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            TypeDescriptor.AddAttributes(typeof(Vector4), new TypeConverterAttribute(typeof(Vector4Converter)));

            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetCompatibleTextRenderingDefault(false);

            PluginLoader.LoadPlugins();

            fileModeForm = new MainForm();
            folderModeForm = new FileView();

            if (args.Length == 0)
            {
                bool? preferFolderMode = null;

                object? value = Registry.GetValue(
                    $@"HKEY_CURRENT_USER\{RegistryKeyPath}",
                    RegistryValueName,
                    null);

                if (value is int i)
                    preferFolderMode = i == 1;

                if (preferFolderMode is null)
                {
                    DialogResult result = MessageBox.Show(
                        "Welcome to Bundle Manager!" +
                        "\n\nThis program has 2 interchangable modes, Studio mode and Single Bundle mode." +
                        "\n\nSingle Bundle mode is better for quicker, individual edits, while Studio mode is designed to easily access many bundles." +
                        "\n\nWould you like to start in Studio mode?" +
                        "\n\nThis setting can be changed at any time by clicking the \"Switch to (Bundle/Studio) Mode\" at the top of the main window.",
                        "Question",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Cancel)
                        return;

                    bool useFolderMode = result == DialogResult.Yes;

                    SavePreferredMode(useFolderMode ? true : false);

                    if (useFolderMode)
                        Application.Run(new FileView());
                    else
                        Application.Run(new MainForm());
                }
                else
                {
                    if (preferFolderMode.Value)
                        Application.Run(new FileView());
                    else
                        Application.Run(new MainForm());
                }

                return;
            }
            else
            {
                if (args.Length >= 1)
                {
                    string path = args[0];
                    if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                    {
                        folderModeForm.Open(path);
                        Application.Run(folderModeForm);
                    }
                    else
                    {
                        fileModeForm.Open(path);
                        if (args.Length >= 2)
                        {
                            string indexString = args[1];
                            if (int.TryParse(indexString, out int index))
                            {
                                fileModeForm.ForceOnlySpecificEntry = true;
                                fileModeForm.EditEntry(index);
                            }
                        }
                        Application.Run(fileModeForm);
                    }
                }
            }

        }

        public static void SavePreferredMode(bool useFolderMode)
        {
            Registry.SetValue(
                $@"HKEY_CURRENT_USER\{RegistryKeyPath}",
                RegistryValueName,
                useFolderMode ? 1 : 0,
                RegistryValueKind.DWord);
        }
    }
}
