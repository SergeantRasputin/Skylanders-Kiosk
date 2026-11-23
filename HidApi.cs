using System;
using System.Runtime.InteropServices;

namespace Com.Skylanders.Kiosk
{
    
    internal static class HidApi
    {
        private const string Dll = "hidapi";
        private const CallingConvention Cc = CallingConvention.Cdecl;

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern int hid_init();

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern int hid_exit();

        // const wchar_t* serial_number -> pass IntPtr.Zero for "any".
        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern IntPtr hid_open(ushort vendor_id, ushort product_id, IntPtr serial_number);

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern void hid_close(IntPtr device);

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern int hid_set_nonblocking(IntPtr device, int nonblock);

        // size_t length -> UIntPtr (64-bit on x64).
        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern int hid_write(IntPtr device, byte[] data, UIntPtr length);

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern int hid_read(IntPtr device, byte[] data, UIntPtr length);

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern int hid_read_timeout(IntPtr device, byte[] data, UIntPtr length, int milliseconds);

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern IntPtr hid_enumerate(ushort vendor_id, ushort product_id);

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern void hid_free_enumeration(IntPtr devs);

        [DllImport(Dll, CallingConvention = Cc)]
        internal static extern IntPtr hid_error(IntPtr device);
    }
}
