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
                Helpers.byteArrayToInt byteArray

            printfn "IConnection msgSize: %O" msgSize
            
            // Get msg
            let msg =
                let byteArray = Array.zeroCreate msgSize
                stream.Read(byteArray, 0, msgSize) |> ignore
                Helpers.byteArrayToString byteArray

            printfn "IConnection receive: %O" msg
            msg

        member this.Send (msg: Message) =
            let msgByteArray = Helpers.stringToByteArray msg
            let msgSizeByteArray = Helpers.intToByteArray (msgByteArray.Length)

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
            printfn "New connection!"
            new TcpConnection(tcpClient) :> IConnection