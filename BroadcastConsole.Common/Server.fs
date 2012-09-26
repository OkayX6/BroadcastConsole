namespace BroadcastConsole.Common

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces

type private Agent<'T> = MailboxProcessor<'T>

/// An in memory server that listens
type Server(listener: IConnectionListener) as this =
    let channelListeners = new Dictionary<string, Event<Message>>()
    let channelExists name = channelListeners.ContainsKey(name)
    let triggerUnknownChannelExn name =
        failwith <| sprintf "Unknown channel name: %O" name

    let lockObject = new Object()
    //let asyncAccept () = async { return! listener.Accept |> Helpers.toAsync }
    let tokenSource = new CancellationTokenSource()
    let inputChannels = new Dictionary<IConnection, string>()
    let outputChannels = new Dictionary<string, Event<Message>>()

    let agent =
        let rec agentLoop (mbox: Agent<string * Message>) =
            async {
                let! channelName, msg = mbox.Receive()

                try
                    lock(lockObject)
                        (fun () ->
                            if outputChannels.ContainsKey(channelName) then
                                outputChannels.[channelName].Trigger(msg))
                with
                    _ -> ()

                do! agentLoop mbox
            }
        
        Agent.Start(agentLoop, tokenSource.Token)

    let rec processChannel (connection: IConnection) =
        async {
            let channelName = connection.Receive()

            this.AddChannelReceiver(channelName, connection.Send)
        }

    let processChannelTask (connection: IConnection) =
        let channelName = connection.Receive()
        this.AddChannelReceiver(channelName, connection.Send)

    let rec acceptConnectionLoop () =
        async {
            try
                let channel = listener.Accept()
                Async.Start (processChannel channel)
            with
                _ -> ()

            return! acceptConnectionLoop ()
        }

    let token = tokenSource.Token

    let task =
        new Task(
            fun () ->
                while not token.IsCancellationRequested do
                    try
                        let channel = listener.Accept()
                        Task.Factory.StartNew(new Action(fun _ -> processChannelTask channel)) |> ignore
                    with
                        _ -> ())

    do
        task.Start()
//        acceptConnectionLoop ()
//        |> Async.Start

    member this.SendMessage (channelName: string, msg: Message) =
        agent.Post(channelName, msg)

    member private this.AddChannelSender (connection: IConnection, channelName: string) =
        lock
            (lockObject)
            (fun () -> inputChannels.Add(key = connection, value = channelName))

    member private this.AddChannelReceiver (channelName: string, channelMessageSender: Message -> unit) =
        lock(lockObject)
            (fun () ->
                if not <| outputChannels.ContainsKey(channelName) then
                    outputChannels.Add(key = channelName, value = new Event<Message>())

                Event.add channelMessageSender
                          outputChannels.[channelName].Publish
            )

    interface IDisposable with
        member this.Dispose() =
            tokenSource.Cancel()