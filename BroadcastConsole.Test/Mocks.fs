module BroadcastConsole.Test.Mocks

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Net.Sockets
open System.Threading
open BroadcastConsole.Common
open BroadcastConsole.Common.Desktop
open BroadcastConsole.Common.Interfaces

type PrinterMessage =
    | Stop of AsyncReplyChannel<unit>
    | Message of int * DateTime * string

let rec asyncLoop (mbox: MailboxProcessor<PrinterMessage>) =
    async {
        let! msg = mbox.Receive()
        
        match msg with
        | Message(id, date, msg) -> printfn "[#%O][%O] %O" id (date.ToString("H:mm:ss.f")) msg
        | Stop channel ->
            printfn "[%O] Stop" (DateTime.Now.ToString("H:mm:ss.f"))
            channel.Reply()
        
        do! asyncLoop mbox
    }

let agent : MailboxProcessor<_> = MailboxProcessor.Start(asyncLoop)

let printer fmt =
    let now = DateTime.Now
    let threadId = System.Threading.Thread.CurrentThread.ManagedThreadId
    Printf.kprintf (fun msg -> agent.Post (Message(threadId, now, msg))) fmt

let endPrinterSession () =
    agent.PostAndReply (fun buildMessage -> Stop buildMessage)

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
        lock(enqueueLock)
            (fun _ ->
                ccQueue.Enqueue(x)
                autoResetEvent.Set() |> ignore)

    member this.Dequeue () =
        lock(dequeueLock)
            (fun _ ->
                if ccQueue.IsEmpty then
                    autoResetEvent.WaitOne() |> ignore

                let result =
                    match ccQueue.TryDequeue() with
                    _, res -> res

                lock(enqueueLock)
                    (fun _ ->
                        if ccQueue.Count = 0 then
                            autoResetEvent.Reset() |> ignore)

                result)

let SubscribersPort = 2009
let PublishersPort = 2010

//type TestConnectionListener(port: int) =
//    let listener = TcpConnectionListener(port) :> IConnectionListener
//
//    let mutable gotConnection = false
//    let mutable connectionCount = 0
//
//    member val GotConnection = gotConnection
//    member val ConnectionCount = 0
//
//    interface IConnectionListener with
//        member this.Accept () =
//            let result = listener.Accept()
//            gotConnection <- true
//            connectionCount <- connectionCount + 1
//            result
//
//type TestConnection(port: int) =
//    inherit TcpConnection("127.0.0.1", port)

type ConnectionMock(listener: ConnectionListenerMock, name: string) as this =
    let connectionName = Guid.NewGuid().ToString().Substring(0, 8)
    do
        printer "%O: ConnectionMock(Channel = %O)" connectionName name

    let history = new ResizeArray<Message>()
    let oppositeConnection = new OppositeConnectionMock(this)
    let iconnection = this :> IConnection
    let messageQueue = new ConcurrentQueueWrapper<_>()
    let messageHistory = new ResizeArray<_>()
    let sendMessage msg =
        messageQueue.Enqueue(msg)

    do
        listener.Enqueue(oppositeConnection)
        sendMessage name

    /// Default constructor with default channel name
    new (listener: ConnectionListenerMock) =
        new ConnectionMock(listener, "DefaultChannelName")

    member val ChannelIsRequestedHandle = new ManualResetEventSlim()
    member val MessageQueue : ConcurrentQueueWrapper<_> = messageQueue
    member val MessageHistory : ResizeArray<_> = messageHistory
    
    member this.WaitToBeRequested () =
        let result = this.ChannelIsRequestedHandle.Wait(1000)
        if not result then
            failwith "Channel was not requested"

    member this.Receive () = iconnection.Receive()
    member this.Send (msg) = iconnection.Send(msg)

    interface IConnection with
        member this.Receive () =
            let msg = oppositeConnection.MessageQueue.Dequeue()
            this.MessageHistory.Add(msg)
            printer "Received message: %O (count = %O)" msg (this.MessageHistory.Count)
            msg

        member this.Send (msg: Message) =
            sendMessage name
            printer "[%O][%O] %O" connectionName name msg

        member this.Dispose () = ()

and OppositeConnectionMock(connection: ConnectionMock) =
    let history = new ResizeArray<Message>()
    let messageQueue = new ConcurrentQueueWrapper<Message>()

    do
        printer "OppositeConnectionMock()"

    member val SourceConnection : ConnectionMock = connection
    member val MessageQueue : ConcurrentQueueWrapper<_> = messageQueue

    interface IConnection with
        member this.Receive () =
            let msg = this.SourceConnection.MessageQueue.Dequeue()
            this.SourceConnection.ChannelIsRequestedHandle.Set()
            msg

        member this.Send (msg: Message) =
            printer "Server sends message: %O" msg
            messageQueue.Enqueue(msg)

        member this.Dispose () = ()

and ConnectionListenerMock() =
    let connectionQueue = new ConcurrentQueueWrapper<OppositeConnectionMock>()
    let connections = new ConcurrentQueue<OppositeConnectionMock>()
    let mutable gotConnection = false
    let mutable waitHandle = new ManualResetEventSlim(false)
    let locker = new Object()

    do
        printer "Listener created"

    member val ConnectionQueueCount : int = connectionQueue.Count
    member this.WaitConnectionCount (n: int) =
        lock (locker)
             (fun _ ->
                let mutable timeout = false
                while connections.Count < n && not timeout do
                    timeout <- not <| Monitor.Wait(locker, 50)

                if connections.Count < n then
                    false
                else
                    true)

    member this.WaitConnection() =
        match waitHandle.Wait(1000) with
        | true -> ()
        | false -> failwith "Waited connection without success"

    member this.Enqueue connection = connectionQueue.Enqueue(connection)
    member this.ConnectionCount : int = connections.Count
    member this.GotConnection : bool = gotConnection
    member this.OpenedConnections : _[] = connections.ToArray()

    interface IConnectionListener with
        member this.Accept () =
            printer "ConnectionListener.TryingToDequeue()"
            let connection = connectionQueue.Dequeue()
            
            gotConnection <- true
            waitHandle.Set()
            lock(locker)
                (fun () ->
                    connections.Enqueue(connection)
                    Monitor.Pulse(locker))

            printer "ConnectionListener.Accept()"

            connection :> IConnection