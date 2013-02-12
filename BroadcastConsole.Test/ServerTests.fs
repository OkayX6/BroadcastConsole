namespace BroadcastConsole.Test

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.VisualStudio.TestTools.UnitTesting
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces
open BroadcastConsole.Common.Helpers
open BroadcastConsole.Test.Mocks
open TestUtils

[<TestClass>]
type ServerTests() =
    let wait (ms: int) = Thread.Sleep(millisecondsTimeout = ms)
    let shortWait () = wait 25

    let functionTimesOut (f: unit -> _) =
        let task = Task.Factory.StartNew(new Func<_>(f))
        if task.Wait(25) then
            false
        else
            true

    [<TestMethod>]
    member this.Server_Accepts_OneSubscriber() =
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)

        (?-) listener.GotConnection

        let connection = new ConnectionMock(listener)
        do shortWait ()

        (?+) listener.GotConnection
        (?-) <| listener.WaitConnectionCount 2

    [<TestMethod>]
    member this.Server_Accepts_ManySubscribers() =
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        do shortWait ()

        let c1 = new ConnectionMock(listener)
        let c2 = new ConnectionMock(listener)
        let c3 = new ConnectionMock(listener)
        do shortWait ()
        
        (?+) <| listener.WaitConnectionCount 3
        (?-) <| listener.WaitConnectionCount 4

    [<TestMethod>]
    member this.Subscriber_WhenConnected_IsRequestedChannelName() =
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        do shortWait ()

        let connection = new ConnectionMock(listener)
        do shortWait ()

        connection.WaitToBeRequested()

    [<TestMethod>]
    member this.Subscribers_WhenConnected_AreRequestedTheirChannelName() =
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        let connections =
            [
                new ConnectionMock(listener)
                new ConnectionMock(listener)
                new ConnectionMock(listener)
            ]
        do shortWait ()

        (?+) <| listener.WaitConnectionCount 3

        listener.OpenedConnections
        |> Seq.map (fun oppCon -> oppCon.SourceConnection)
        |> Seq.iter (fun srcCon -> srcCon.WaitToBeRequested())

    [<TestMethod>]
    member this.Subscriber_WhenServerSendsMessages_ToCorrectChannel_IsNotified() =
        let ChannelName = "Channel"
        let Message1 = "hello"
        let Message2 = "denis"
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        let connection = new ConnectionMock(listener, ChannelName)
        do shortWait ()

        connection.WaitToBeRequested()

        // Send message to a wrong and the correct one
        server.SendMessage(ChannelName + "0", Message1)
        server.SendMessage(ChannelName, Message1)
        server.SendMessage(ChannelName, Message2)
        do shortWait ()

        let msg1 = connection.Receive()
        let msg2 = connection.Receive()

        (?+) (functionTimesOut connection.Receive)
        Message1 == msg1
        Message2 == msg2

    [<TestMethod>]
    member this.Subscriber_WhenServerSendsMessages_ToOtherChannels_IsNotNotified() =
        let ChannelName = "Channel"
        let Message1 = "hello"
        let Message2 = "denis"
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        let connection = new ConnectionMock(listener, ChannelName) :> IConnection
        do shortWait ()

        // Send message to a wrong and the correct one
        server.SendMessage(ChannelName + "0", Message1)
        server.SendMessage(ChannelName + "1", Message1)
        do shortWait ()

        (?+) (functionTimesOut connection.Receive)

    //[<TestMethod>]
    member this.Subscribers_WhenServerSendsMessages_ToCorrectChannel_IsNotified() =
        let Channel1 = "Channel1"
        let Channel2 = "Channel2"
        let Channel3 = "Channel3"
        let Message = "hello"
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        do shortWait ()

        let c1 = new ConnectionMock(listener, Channel1)
        let c2 = new ConnectionMock(listener, Channel2)
        let c3 = new ConnectionMock(listener, Channel3)
        do shortWait ()

        (?+) <| listener.WaitConnectionCount 3
        (?-) <| listener.WaitConnectionCount 4
        
        // Send message to a wrong and the correct one
        server.SendMessage(Channel1, Message)
        server.SendMessage(Channel2, Message)
        server.SendMessage(Channel3, Message)
        do shortWait ()

        for connection in [c1; c2; c3] do
            let msg = connection.Receive()

            (?+) (functionTimesOut connection.Receive)
            Message == msg

    //[<TestMethod>]
    member this.Subscribers_ToSameChannel_WhenServerSendsMessages_AreNotified() =
        let ChannelName = "Channel"
        let MESSAGES = ["1"; "2"; "3"]
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        let connections =
            [
                new ConnectionMock(listener, ChannelName)
                new ConnectionMock(listener, ChannelName)
                new ConnectionMock(listener, ChannelName)
            ]

        do shortWait ()

        (?+) <| listener.WaitConnectionCount 3

        server.SendMessage(ChannelName + "1", "heyhey")

        for msg in MESSAGES do
            server.SendMessage(ChannelName, msg)
        do shortWait ()

        for oppCon in listener.OpenedConnections do
            let connection = oppCon.SourceConnection

            connection.Receive() |> ignore
            connection.Receive() |> ignore
            connection.Receive() |> ignore
            (?+) (functionTimesOut connection.Receive)

            let history = connection.MessageHistory
            let sendMsgSet = Set.ofSeq MESSAGES
            let receivedMsgSet = Set.ofSeq history

            3 == history.Count
            sendMsgSet == receivedMsgSet

//    [<TestMethod>]
//    member this.Server_KnowsWhenSubscriberConnectionIsLost() =
//        Assert.Inconclusive()
//
//    [<TestMethod>]
//    member this.Server_KnowsWhenPublisherConnectionIsLost() =
//        Assert.Inconclusive()
//
//    [<TestMethod>]
//    member this.Server_WhenOpeningAndClosingConnectionsWithSubscribers_IsStableInMemory() =
//        Assert.Inconclusive()
//
//    [<TestMethod>]
//    member this.Server_WhenOpeningAndClosingConnectionsWithPublishers_IsStableInMemory() =
//        Assert.Inconclusive()