module BroadcastConsole.Test.Mocks

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces


let private waitOrFail condition =
    let mutable i = 0
    while i < 5 && condition () do
        Thread.Sleep 0
        i <- i + 1

    if condition () then
        failwith "Timeout"

type ConnectionMock(listener: ConnectionListenerMock, name: string) as this =
    let connectionName = Guid.NewGuid().ToString().Substring(0, 8)
    let history = new ResizeArray<Message>()
    let oppositeConnection = new OppositeConnectionMock(this)

    do
        listener.ConnectionQueue.Enqueue(oppositeConnection)
        listener.WaitHandle.Reset() |> ignore

    /// Default constructor with default channel name
    new (listener: ConnectionListenerMock) =
        new ConnectionMock(listener, "DefaultChannelName")

    member val IsRegistered : bool = false with get, set
    member val MessageQueue = new Queue<Message>()
    member val MessageHistory = new ResizeArray<Message>()

    interface IConnection with
        member this.Receive () =
            waitOrFail (fun () -> oppositeConnection.MessageQueue.Count = 0)

            let msg = oppositeConnection.MessageQueue.Dequeue()
            this.MessageHistory.Add(msg)
            msg

        member this.Send (msg: Message) =
            this.MessageQueue.Enqueue(msg)
            this.MessageHistory.Add(msg)
            printfn "[%O][%O] %O" connectionName name msg

        member this.Dispose () = ()

and OppositeConnectionMock(connection: ConnectionMock) =
    let history = new ResizeArray<Message>()
    let messageQueue = new Queue<Message>()

    member val SourceConnection : ConnectionMock = connection
    member val MessageQueue : Queue<_> = messageQueue

    interface IConnection with
        member this.Receive () =
            waitOrFail (fun () -> this.SourceConnection.MessageQueue.Count = 0)

            this.SourceConnection.IsRegistered <- true
            this.SourceConnection.MessageQueue.Dequeue()

        member this.Send (msg: Message) =
            messageQueue.Enqueue(msg)

        member this.Dispose () = ()

and ConnectionListenerMock() =
    let connectionQueue = new ConcurrentQueue<OppositeConnectionMock>()
    let connections = new ResizeArray<OppositeConnectionMock>()

    member val WaitHandle : AutoResetEvent = new AutoResetEvent(false)
    member this.ConnectionQueue : ConcurrentQueue<_> = connectionQueue
    member this.ConnectionCount : int = connections.Count
    member this.GotConnection : bool = connections.Count > 0
    member this.OpenedConnections : OppositeConnectionMock[] =
        connections.ToArray()

    interface IConnectionListener with
        member this.Accept () =
            this.WaitHandle.WaitOne() |> ignore

            let _, result  = this.ConnectionQueue.TryDequeue()
            connections.Add(result)

            result :> IConnection