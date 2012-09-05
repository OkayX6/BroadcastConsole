namespace BroadcastConsole.Common

open System
open System.Collections.Generic
open System.Threading
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
    let asyncAccept () = async { return! listener.Accept |> Helpers.toAsync }
    let tokenSource = new CancellationTokenSource()
    let inputChannels = new Dictionary<IConnection, string>()
    let outputChannels = new Dictionary<string, Event<Message>>()

    let agent =
        let rec agentLoop (mbox: Agent<string * Message>) =
            async {
                let! channelName, msg = mbox.Receive()

                lock(lockObject)
                    (fun () ->
                        if outputChannels.ContainsKey(channelName) then
                            outputChannels.[channelName].Trigger(msg))

                do! agentLoop mbox
            }
        
        Agent.Start(agentLoop, tokenSource.Token)

    let rec processChannel (connection: IConnection) =
        async {
            let asyncReceive = connection.Receive |> Helpers.toAsync
            let! channelName = asyncReceive

            this.AddChannelReceiver(channelName, connection.Send)
        }

    let rec acceptConnectionLoop () =
        async {
            try
                let context = SynchronizationContext.Current

                do! Async.SwitchToNewThread()
                let channel = listener.Accept()
                do! Async.SwitchToContext(context)

                Async.Start (processChannel channel)
            with
                _ -> ()

            return! acceptConnectionLoop ()
        }

    do
        acceptConnectionLoop ()
        |> Async.Start

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