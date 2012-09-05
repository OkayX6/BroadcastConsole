module BroadcastConsole.Test.Mocks

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces

let private waitOrFail condition =
    let mutable i = 0
    while i < 10 && condition () do
        Thread.Sleep 50
        i <- i + 1

    if condition () then
        failwith "Timeout"

type ConcurrentQueueWrapper<'a>() =
    let ccQueue = new ConcurrentQueue<'a>()
    let enqueueLock = new Object()
    let dequeueLock = new Object()
    // false <= queue is empty
    // that means you have to wait until you can dequeue
    let autoResetEvent = new AutoResetEvent(false)

    member val Count = ccQueue.Count
    member this.Enqueue x =
        //printfn "Enqueue: try lock ENQUEUE"
        lock(enqueueLock)
            (fun _ ->
                //printfn "Enqueue: lock ENQUEUE"
                ccQueue.Enqueue(x)
                autoResetEvent.Set() |> ignore)
        //printfn "Enqueue: unlock ENQUEUE"

    member this.Dequeue () =
        lock(dequeueLock)
            (fun _ ->
                if ccQueue.IsEmpty then
                    autoResetEvent.WaitOne() |> ignore

                let result =
                    match ccQueue.TryDequeue() with
                    _, res -> res

                //printfn "Dequeue: try lock ENQUEUE"
                lock(enqueueLock)
                    (fun _ ->
                        //printfn "Dequeue: lock ENQUEUE"
                        if ccQueue.Count = 0 then
                            autoResetEvent.Reset() |> ignore)
                //printfn "Dequeue: unlock ENQUEUE"

                result)

type ConnectionMock(listener: ConnectionListenerMock, name: string) as this =
    let connectionName = Guid.NewGuid().ToString().Substring(0, 8)
    let history = new ResizeArray<Message>()
    let oppositeConnection = new OppositeConnectionMock(this)
    let iconnection = this :> IConnection
    let messageQueue = new ConcurrentQueueWrapper<_>()
    let messageHistory = new ResizeArray<_>()
    let sendMessage msg =
        messageQueue.Enqueue(msg)

    do
        listener.Enqueue(oppositeConnection)        
        printfn "%O: ConnectionMock(Channel = %O)" connectionName name

        sendMessage name

    /// Default constructor with default channel name
    new (listener: ConnectionListenerMock) =
        new ConnectionMock(listener, "DefaultChannelName")

    member val ChannelNameIsRequested : bool = false with get, set
    member val MessageQueue : ConcurrentQueueWrapper<_> = messageQueue
    member val MessageHistory : ResizeArray<_> = messageHistory

    member this.Receive () = iconnection.Receive()
    member this.Send (msg) = iconnection.Send(msg)

    interface IConnection with
        member this.Receive () =
            let msg = oppositeConnection.MessageQueue.Dequeue()
            this.MessageHistory.Add(msg)
            printfn "Received message: %O (count = %O)" msg (this.MessageHistory.Count)
            msg

        member this.Send (msg: Message) =
            sendMessage name
            printfn "[%O][%O] %O" connectionName name msg

        member this.Dispose () = ()

and OppositeConnectionMock(connection: ConnectionMock) =
    let history = new ResizeArray<Message>()
    let messageQueue = new ConcurrentQueueWrapper<Message>()

    do
        printfn "OppositeConnectionMock()"

    member val SourceConnection : ConnectionMock = connection
    member val MessageQueue : ConcurrentQueueWrapper<_> = messageQueue

    interface IConnection with
        member this.Receive () =
            let msg = this.SourceConnection.MessageQueue.Dequeue()
            this.SourceConnection.ChannelNameIsRequested <- true
            msg

        member this.Send (msg: Message) =
            printfn "Server sends message: %O" msg
            messageQueue.Enqueue(msg)

        member this.Dispose () = ()

and ConnectionListenerMock() =
    let connectionQueue = new ConcurrentQueueWrapper<OppositeConnectionMock>()
    let connections = new ResizeArray<OppositeConnectionMock>()

    do
        printfn "%O" DateTime.Now

    member val ConnectionQueueCount : int = connectionQueue.Count
    member this.Enqueue connection = connectionQueue.Enqueue(connection)
    member this.ConnectionCount : int = connections.Count
    member this.GotConnection : bool = connections.Count > 0
    member this.OpenedConnections : OppositeConnectionMock[] =
        connections.ToArray()

    interface IConnectionListener with
        member this.Accept () =
            Console.WriteLine("ConnectionListener.TryingToDequeue()")
            let connection = connectionQueue.Dequeue()

            printfn "ConnectionListener.Accept()"
            printfn "ConnectionQueue = %O" connectionQueue.Count
            
            connections.Add(connection)
            connection :> IConnection