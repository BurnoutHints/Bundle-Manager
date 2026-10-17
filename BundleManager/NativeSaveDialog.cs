using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BundleManager
{
    internal static class NativeSaveDialog
    {
        private const uint FOS_OVERWRITEPROMPT = 0x2;
        private const uint FOS_FORCEFILESYSTEM = 0x40;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
        private const int ERROR_CANCELLED = unchecked((int)0x800704C7);

        public static string Show(IWin32Window owner, string fileName, string defaultExtension, string filter, Func<int, string, string> onTypeChange, out int typeIndex)
        {
            IFileDialog dialog = (IFileDialog)new FileSaveDialog();
            try
            {
                string[] parts = filter.Split('|');
                FilterSpec[] types = Enumerable.Range(0, parts.Length / 2)
                    .Select(i => new FilterSpec { Name = parts[i * 2], Spec = parts[i * 2 + 1] })
                    .ToArray();

                dialog.SetFileTypes((uint)types.Length, types);
                dialog.SetFileTypeIndex(1);
                dialog.SetDefaultExtension(defaultExtension);
                dialog.SetFileName(fileName);
                dialog.GetOptions(out uint options);
                dialog.SetOptions(options | FOS_OVERWRITEPROMPT | FOS_FORCEFILESYSTEM);

                dialog.Advise(new Events(onTypeChange), out uint cookie);
                try
                {
                    int hr = dialog.Show(owner?.Handle ?? IntPtr.Zero);
                    dialog.GetFileTypeIndex(out uint index);
                    typeIndex = (int)index;

                    if (hr == ERROR_CANCELLED)
                        return null;
                    Marshal.ThrowExceptionForHR(hr);

                    dialog.GetResult(out IShellItem item);
                    item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
                    return path;
                }
                finally
                {
                    dialog.Unadvise(cookie);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(dialog);
            }
        }

        private sealed class Events(Func<int, string, string> onTypeChange) : IFileDialogEvents
        {
            private const int E_NOTIMPL = unchecked((int)0x80004001);

            public int OnTypeChange(IFileDialog pfd)
            {
                // The dialog rewrites the name's extension after this returns, so rename once it's done
                if (SynchronizationContext.Current is SynchronizationContext context)
                    context.Post(_ => Rename(pfd), null);
                else
                    Rename(pfd);
                return 0;
            }

            private void Rename(IFileDialog pfd)
            {
                pfd.GetFileTypeIndex(out uint index);
                pfd.GetFileName(out string name);
                string newName = onTypeChange((int)index, name ?? string.Empty);
                if (newName != name)
                    pfd.SetFileName(newName);
            }

            public int OnFileOk(IFileDialog pfd) => 0;
            public int OnFolderChanging(IFileDialog pfd, IShellItem psiFolder) => 0;
            public int OnFolderChange(IFileDialog pfd) => 0;
            public int OnSelectionChange(IFileDialog pfd) => 0;
            public int OnShareViolation(IFileDialog pfd, IShellItem psi, out int pResponse) { pResponse = 0; return E_NOTIMPL; }
            public int OnOverwrite(IFileDialog pfd, IShellItem psi, out int pResponse) { pResponse = 0; return E_NOTIMPL; }
        }
    }

    [ComImport, Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
    internal class FileSaveDialog { }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct FilterSpec
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string Name;
        [MarshalAs(UnmanagedType.LPWStr)] public string Spec;
    }

    [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IFileDialogEvents pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
    }

    [ComImport, Guid("973510db-7d7f-452b-8975-74a85828d354"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialogEvents
    {
        [PreserveSig] int OnFileOk(IFileDialog pfd);
        [PreserveSig] int OnFolderChanging(IFileDialog pfd, IShellItem psiFolder);
        [PreserveSig] int OnFolderChange(IFileDialog pfd);
        [PreserveSig] int OnSelectionChange(IFileDialog pfd);
        [PreserveSig] int OnShareViolation(IFileDialog pfd, IShellItem psi, out int pResponse);
        [PreserveSig] int OnTypeChange(IFileDialog pfd);
        [PreserveSig] int OnOverwrite(IFileDialog pfd, IShellItem psi, out int pResponse);
    }

    [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }
}
