namespace BroadcastConsole.Common.Desktop

open System
open System.Net
open System.Net.Sockets
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces


type TcpConnection(tcpClient: TcpClient) =
    let stream = tcpClient.GetStream()

    let writeToStream (bytes: byte[]) =
        stream.Write(bytes, 0, bytes.Length)

    new (hostName: string, port: int) =
        let tcpClient = new TcpClient(hostName, port)
        new TcpConnection(tcpClient)

    interface IConnection with
        member this.Receive () =
            // Get msg size in bytes
            let msgSize =
                let byteArray = Array.zeroCreate 4
                do stream.Read(byteArray, 0, 4) |> ignore
                Helpers.intOfByteArray byteArray
            
            // Get msg
            let msg =
                let byteArray = Array.zeroCreate msgSize
                stream.Read(byteArray, 0, msgSize) |> ignore
                Helpers.stringOfByteArray byteArray

            msg

        member this.Send (msg: Message) =
            let msgByteArray = Helpers.byteArrayOfString msg
            let msgSizeByteArray = Helpers.byteArrayOfInt (msgByteArray.Length)

            // Write message size
            writeToStream msgSizeByteArray
            // Write message
            writeToStream msgByteArray

        member this.Dispose () =
            tcpClient.GetStream().Close()
            tcpClient.Close()


type TcpConnectionListener(port: int) =
    let tcpListener = new TcpListener(IPAddress.Any, port)
    do
        tcpListener.Start()

    interface IConnectionListener with
        member this.Accept (): IConnection =
            let tcpClient = tcpListener.AcceptTcpClient()
            new TcpConnection(tcpClient) :> IConnection