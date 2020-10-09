using System;
using System.Runtime.InteropServices;

namespace N1T1Tool
{
    [StructLayout(LayoutKind.Sequential)]
    struct Device
    {
        public const int Size = 662;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string ServerIP;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string ClientIP;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Netmask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Gateway;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string PrimaryDNS;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string SecondaryDNS;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string MACAdress;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string ServerName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ServerDescription;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Workgroup;

        public byte Wins;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string WinsIP;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ServerPassword;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ServerModelName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ServerSerial;

        public byte OpCode;

        public byte OpResult;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string DDNSUserName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string DDNSPassword;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ServerDDNSName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ModelName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ModelFirmwareVersion;

        public byte UPnPEnabled;

        public byte FTPEnabled;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string FTPPort;

        public byte DDNSEnabled;

        public static implicit operator byte[](Device structure)
        {
            int size = Marshal.SizeOf(structure);
            byte[] destination = new byte[size];
            IntPtr source = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, source, true);
            Marshal.Copy(source, destination, 0, size);
            Marshal.FreeHGlobal(source);
            return destination;
        }

        public static implicit operator Device(byte[] source)
        {
            Device structure = new Device();
            int size = Marshal.SizeOf(structure);

            if (size == source.Length)
            {
                IntPtr destination = Marshal.AllocHGlobal(size);
                Marshal.Copy(source, 0, destination, size);
                structure = (Device)Marshal.PtrToStructure(destination, typeof(Device));
                Marshal.FreeHGlobal(destination);
                return structure;
            }

            throw new ArgumentException("Invalid Size");
        }
    }
}
