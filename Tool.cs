using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Tftp.Net;

namespace N1T1Tool
{
    class Tool
    {
        private const int LocalPort = 24987;
        private const int RemotePort = 24988;

        private TftpServer m_Server;
        private UdpClient m_Client;
        private IPEndPoint m_Remote;
        private Device m_Device;
        private string m_Path;

        public Tool()
        {
            m_Remote = new IPEndPoint(IPAddress.Broadcast, RemotePort);
        }

        public int StatusCode { get; private set; }

        private bool RequestDeviceInfo()
        {
            try
            {
                m_Client.Send(new Device() { OpCode = (byte)OpCode.RequestInfo }, Device.Size, m_Remote);
                m_Device = m_Client.Receive(ref m_Remote);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ConfigureDeviceForDHCP()
        {
            try
            {
                m_Device.OpCode = (byte)OpCode.RequestDHCP;
                m_Client.Send(m_Device, Device.Size, m_Remote);
                m_Device = m_Client.Receive(ref m_Remote);
                return m_Device.OpCode == (byte)OpCode.DHCPOk;
            }
            catch
            {
                return false;
            }
        }

        private bool InitiateDeviceUpdate()
        {
            try
            {
                m_Device.OpCode = (byte)OpCode.RequestUpdate;
                m_Client.Send(m_Device, Device.Size);
                m_Device = m_Client.Receive(ref m_Remote);
                return m_Device.OpCode == (byte)OpCode.RequestUpdate;
            }
            catch
            {
                return false;
            }
        }

        public void SendInitrd(string path)
        {
            if (File.Exists(path))
            {
                using (m_Server = new TftpServer())
                {
                    m_Server.OnReadRequest += OnReadRequest;

                    using (m_Client = new UdpClient(new IPEndPoint(IPAddress.Any, LocalPort)))
                    {
                        if (!RequestDeviceInfo() || !ConfigureDeviceForDHCP())
                        {
                            return;
                        }

                        if (InitiateDeviceUpdate())
                        {
                            m_Path = path;
                            m_Server.Start();
                        }
                    }
                }
            }
        }

        private void OnReadRequest(ITftpTransfer transfer, EndPoint client)
        {
            if (transfer.Filename == "uboot.bin" || transfer.Filename == "uImage_nt1_netenc")
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(transfer.Filename))
                {
                    transfer.Start(stream);
                }
            }
            else if (transfer.Filename == m_Path)
            {
                using (FileStream stream = new FileStream(transfer.Filename, FileMode.Open))
                {
                    transfer.Start(stream);
                }
            }
            else
            {
                transfer.Cancel(TftpErrorPacket.AccessViolation);
            }
        }

        private enum OpCode
        {
            RequestDHCP = 'd',
            RequestInfo = 'r',
            RequestStaticIP = 'w',
            RequestUpdate = 'U',
            IPGatewayMismatch = 'f',
            DHCPOk = 'o',
            DHCPFail = 'x',
            IPSettingOk = 'g',
            IPTaken = 'i',
            PasswordMismatch = 'p',
        }
    }
}
