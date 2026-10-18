using PluginSystem;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

using BundleManager.TypeConverters;

namespace BundleManager;


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

        TypeDescriptor.AddAttributes(typeof(Vector2), new TypeConverterAttribute(typeof(Vector2Converter)));
        TypeDescriptor.AddAttributes(typeof(Vector4), new TypeConverterAttribute(typeof(Vector4Converter)));

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.SetCompatibleTextRenderingDefault(false);

        PluginLoader.InitializePlugins();

        fileModeForm = new MainForm();
        folderModeForm = new FileView();

        CheckElevation();

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
                TaskDialogCommandLinkButton studio = new("Studio mode",
                    "Edit complex resources, such as models, that need dependent resources found in other bundles.");
                TaskDialogCommandLinkButton bundle = new("Bundle mode",
                    "Make quicker, individual edits to a single bundle.");

                TaskDialogPage page = new()
                {
                    Caption = "Bundle Manager",
                    Heading = "Welcome to Bundle Manager.",
                    Text = "This program has two interchangable modes. Which one would you like to start in?\n\nYou can switch at any time with \"Switch to Bundle/Studio Mode\" at the top of the main window.",
                    Buttons = { bundle, studio, TaskDialogButton.Cancel },
                    Footnote = "",
                    AllowCancel = true
                };

                TaskDialogButton result = TaskDialog.ShowDialog(page);
                if (result != studio && result != bundle)
                    return;

                bool useFolderMode = result == studio;

                SavePreferredMode(useFolderMode);

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

    public static void CheckElevation()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            MessageBox.Show("Bundle Manager is running with standard permissions.\n"
                + "Saving may fail in write-protected locations.",
                "Running non-elevated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
