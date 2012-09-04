using System;
using System.Threading.Tasks;
using BroadcastConsole.Common.Interfaces;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BroadcastConsole.Common.WinRT
{
    public class TcpConnection : IConnection
    {
        private readonly StreamSocket streamSocket;

        public TcpConnection(string hostName, int port)
        {
            this.streamSocket = new StreamSocket();
            this.streamSocket.ConnectAsync(
                remoteHostName: new HostName(hostName),
                remoteServiceName: port.ToString())
                .AsTask()
                .Wait();
        }

        public void Dispose()
        {
            this.streamSocket.Dispose();
        }

        public string Receive()
        {
            IInputStream istream = this.streamSocket.InputStream;
            var dataReader = new DataReader(istream);

            byte[] msgSizeByteArray = new byte[4];
            dataReader.LoadAsync(4).AsTask().Wait();
            dataReader.ReadBytes(msgSizeByteArray);

            int msgSize = Helpers.byteArrayToInt(msgSizeByteArray);

            byte[] msgByteArray = new byte[msgSize];
            dataReader.LoadAsync((uint) msgSize).AsTask().Wait();
            dataReader.ReadBytes(msgByteArray);

            string message = Helpers.byteArrayToString(msgByteArray);
            return message;
        }

        public void Send(string message)
        {
            var ostream = this.streamSocket.OutputStream;
            var dataWriter = new DataWriter(ostream);

            byte[] msgByteArray = Helpers.stringToByteArray(message);
            byte[] msgSizeByteArray = Helpers.intToByteArray(msgByteArray.Length);

            dataWriter.WriteBytes(msgSizeByteArray);
            dataWriter.WriteBytes(msgByteArray);
            dataWriter.StoreAsync().AsTask().Wait();
            dataWriter.FlushAsync().AsTask().Wait();
        }

        private void Wait(IAsyncResult asyncResult)
        {
            Task<IAsyncResult> actionAsync = Task.FromResult(asyncResult);
            actionAsync.Wait();
        }
    }
}
