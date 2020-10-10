using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Tftp.Net;

namespace N1T1Tool
{
    class Tool
    {
        private const int LocalPort = 24987;
        private const int RemotePort = 24988;
        private const string ResourcePath = "N1T1Tool.Resources.";
        private const string UBootName = "uboot.bin";
        private const string UImageName = "uImage_nt1_netenc";

        private TftpServer m_Server;
        private UdpClient m_Client;
        private IPEndPoint m_RemoteBroadcastEP;
        private IPEndPoint m_RemoteEP;
        private Device m_Device;
        private string m_Path;

        public Tool()
        {
            m_RemoteBroadcastEP = new IPEndPoint(IPAddress.Broadcast, RemotePort);
        }

        public int StatusCode { get; private set; }

        private bool RequestDeviceInfo()
        {
            try
            {
                m_Client.Send(new Device() { OpCode = (byte)OpCode.RequestInfo }, Device.Size, m_RemoteBroadcastEP);
                m_Device = m_Client.Receive(ref m_RemoteEP);
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
                m_Client.Send(m_Device, Device.Size, m_RemoteBroadcastEP);
                m_Device = m_Client.Receive(ref m_RemoteEP);
                return m_Device.OpCode == (byte)OpCode.DHCPOk;
            }
            catch
            {
                return false;
            }
        }

        private bool InitiateDeviceUpdate(string path)
        {
            try
            {
                char delimiter = ' ';
                StringBuilder builder = new StringBuilder();
                builder.Append(UImageName);
                builder.Append(delimiter);
                builder.Append(Path.GetFileName(path));
                builder.Append(delimiter);
                builder.Append(0);
                builder.Append(delimiter);
                builder.Append(UBootName);

                m_Device.OpCode = (byte)OpCode.RequestUpdate;
                m_Device.ServerDescription = builder.ToString();
                m_Client.Send(m_Device, Device.Size, m_RemoteBroadcastEP);
                m_Device = m_Client.Receive(ref m_RemoteEP);
                return m_Device.OpCode == (byte)OpCode.RequestInfo;
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
                        if (!RequestDeviceInfo() || !ConfigureDeviceForDHCP() || !RequestDeviceInfo())
                        {
                            return;
                        }

                        if (InitiateDeviceUpdate(path))
                        {
                            m_Path = path;
                            m_Server.Start();
                        }

                        Console.Read();
                    }
                }
            }
        }

        private void OnReadRequest(ITftpTransfer transfer, EndPoint client)
        {
            if (transfer.Filename == UBootName || transfer.Filename == UImageName)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePath + transfer.Filename))
                {
                    transfer.Start(stream);
                }
            }
            else if (transfer.Filename == Path.GetFileName(m_Path))
            {
                using (FileStream stream = new FileStream(m_Path, FileMode.Open))
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
