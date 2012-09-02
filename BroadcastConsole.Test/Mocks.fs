module Mocks

open System
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces

type ConnectionMock(name: string) =
    let connectionName = Guid.NewGuid().ToString().Substring(0, 8)
    let mutable isRegistered = false
    let history = new ResizeArray<Message>()

    new() = new ConnectionMock("DefaultChannelName")

    member this.IsRegistered = isRegistered
    member this.MessageHistory : Message[] =
        history.ToArray()

    interface IConnection with
        member this.Receive () =
            isRegistered <- true
            name

        member this.Send (msg: Message) =
            history.Add(msg)
            printfn "[%O][%O] %O" connectionName name msg

type ConnectionListenerMock(maxConnections: int, gen) =
    let channels = new ResizeArray<ConnectionMock>()
    let mutable channelNameGenerator = gen

    new() = new ConnectionListenerMock(1)

    new(maxConnections: int) =
        let defaultGen () = "DefaultChannelName"
        new ConnectionListenerMock(maxConnections, defaultGen)
    
    new(gen: unit -> string) =
        new ConnectionListenerMock(-1, gen)

    member this.ConnectionCount : int = channels.Count
    member this.GotConnection : bool = channels.Count > 0
    member this.OpenedChannels : ConnectionMock[] =
        channels.ToArray()

    interface IConnectionListener with
        member this.Accept () =
            if (this.ConnectionCount < maxConnections) then
                let name = channelNameGenerator()
                let result = new ConnectionMock(name)

                channels.Add(result)
                result :> IConnection
            else
                failwith "Max connections reached"