using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
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
        private bool m_TransferHasFinished;
        private bool m_TransferHasError;

        public Tool()
        {
            m_RemoteBroadcastEP = new IPEndPoint(IPAddress.Broadcast, RemotePort);
            m_Client.Client.ReceiveTimeout = 3000;
        }

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

        public StatusCode SendInitrd(string path)
        {
            if (File.Exists(path))
            {
                using (m_Server = new TftpServer())
                {
                    m_Server.OnReadRequest += OnReadRequest;

                    using (m_Client = new UdpClient(new IPEndPoint(IPAddress.Any, LocalPort)))
                    {
                        if (!RequestDeviceInfo())
                        {
                            return StatusCode.DeviceNotFound;
                        }

                        if (!ConfigureDeviceForDHCP())
                        {
                            return StatusCode.DHCPConifgureFailed;
                        }

                        if (!RequestDeviceInfo())
                        {
                            return StatusCode.DeviceNotFound;
                        }

                        m_Path = path;
                        m_TransferHasError = false;
                        m_TransferHasFinished = false;
                        m_Server.Start();

                        if (!InitiateDeviceUpdate(path))
                        {
                            return StatusCode.UpdateFailed;
                        }

                        while (!m_TransferHasFinished)
                        {
                            if (m_TransferHasError)
                            {
                                return StatusCode.TFTPFailed;
                            }

                            Thread.Sleep(1000);
                        }

                        return StatusCode.Success;
                    }
                }
            }
            else
            {
                return StatusCode.FileNotFound;
            }
        }

        private void OnReadRequest(ITftpTransfer transfer, EndPoint client)
        {
            if (transfer.Filename == UBootName || transfer.Filename == UImageName)
            {
                transfer.OnError += OnError;
                transfer.Start(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePath + transfer.Filename));
            }
            else if (transfer.Filename == Path.GetFileName(m_Path))
            {
                transfer.OnError += OnError;
                transfer.OnFinished += OnFinished;
                transfer.Start(new FileStream(m_Path, FileMode.Open));
            }
            else
            {
                transfer.Cancel(TftpErrorPacket.AccessViolation);
            }
        }

        private void OnError(ITftpTransfer transfer, TftpTransferError error)
        {
            m_TransferHasError = true;
        }

        private void OnFinished(ITftpTransfer transfer)
        {
            m_TransferHasFinished = true;
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
