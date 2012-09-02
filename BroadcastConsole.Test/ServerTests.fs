namespace BroadcastConsole.Test

open System.Threading
open Microsoft.VisualStudio.TestTools.UnitTesting
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces
open BroadcastConsole.Common.Helpers
open BroadcastConsole.Test.Mocks

[<TestClass>]
type ServerTests() =
    let wait (ms: int) = Thread.Sleep(millisecondsTimeout = ms)
    let shortWait () = wait 10

    [<TestMethod>]
    member this.Server_Accepts_OneSubscriber() =
        let listener = new ConnectionListenerMock()
        
        Assert.IsFalse(listener.GotConnection)

        let server = new Server(listener)
        do shortWait ()

        (?+) listener.GotConnection

    [<TestMethod>]
    member this.Server_Accepts_ManySubscribers() =
        let listener = new ConnectionListenerMock(maxConnections = 4)
        let server = new Server(listener)
        do shortWait ()

        4 == listener.ConnectionCount

    [<TestMethod>]
    member this.Subscriber_WhenConnected_IsRegistered() =
        let listener = new ConnectionListenerMock()
        let server = new Server(listener)
        do shortWait ()

        let channel = listener.OpenedChannels.[0]
        (?+) channel.IsRegistered

    [<TestMethod>]
    member this.Subscribers_WhenConnected_AreRegistered() =
        let MaxConnections = 4
        let listener = new ConnectionListenerMock(MaxConnections)
        let server = new Server(listener)
        do shortWait ()

        Assert.AreEqual(MaxConnections, listener.OpenedChannels.Length)
        listener.OpenedChannels
        |> Seq.iter (fun channel -> Assert.IsTrue(channel.IsRegistered))

    [<TestMethod>]
    member this.Subscriber_WhenServerSendsMessages_ToCorrectChannel_IsNotified() =
        let ChannelName = "Channel"
        let channelNameGenerator () = ChannelName
        let Message1 = "hello"
        let Message2 = "denis"
        let listener = new ConnectionListenerMock(1, channelNameGenerator)
        let server = new Server(listener)
        do shortWait ()

        // Send message to a wrong and the correct one
        server.SendMessage(ChannelName + "0", Message1)
        server.SendMessage(ChannelName, Message1)
        server.SendMessage(ChannelName, Message2)
        do shortWait ()

        let channel = listener.OpenedChannels.[0]
        let history = channel.MessageHistory

        2        == history.Length
        Message1 == history.[0]
        Message2 == history.[1]

    [<TestMethod>]
    member this.Subscriber_WhenServerSendsMessages_ToOtherChannels_IsNotNotified() =
        let ChannelName = "Channel"
        let channelNameGenerator () = ChannelName
        let Message1 = "hello"
        let Message2 = "denis"
        let listener = new ConnectionListenerMock(1, channelNameGenerator)
        let server = new Server(listener)
        do shortWait ()

        // Send message to a wrong and the correct one
        server.SendMessage(ChannelName + "0", Message1)
        server.SendMessage(ChannelName + "1", Message1)
        do shortWait ()

        let channel = listener.OpenedChannels.[0]
        let history = channel.MessageHistory

        0 == history.Length

    [<TestMethod>]
    member this.Subscribers_WhenServerSendsMessages_ToCorrectChannel_IsNotified() =
        let Channel1 = "Channel1"
        let Channel2 = "Channel2"
        let Channel3 = "Channel3"
        let counter = ref 0

        let channelNameGenerator () =
            let result =
                [| Channel1; Channel2; Channel3 |].[!counter % 3]

            counter |> incr
            result

        let ConnectionCount = 3
        let Message = "hello"
        let listener = new ConnectionListenerMock(ConnectionCount, channelNameGenerator)
        let server = new Server(listener)
        do shortWait ()

        // Send message to a wrong and the correct one
        server.SendMessage(Channel1, Message)
        server.SendMessage(Channel2, Message)
        server.SendMessage(Channel3, Message)
        do shortWait ()

        Assert.AreEqual(ConnectionCount, listener.OpenedChannels.Length)

        for channel in listener.OpenedChannels do
            let history = channel.MessageHistory

            Assert.AreEqual(1, history.Length)
            Assert.AreEqual(Message, history.[0])

    [<TestMethod>]
    member this.Subscribers_ToSameChannel_WhenServerSendsMessages_AreNotified() =
        let ChannelName = "Channel"
        let channelNameGenerator () = ChannelName
        let MESSAGES = ["1"; "2"; "3"]
        let listener = new ConnectionListenerMock(4, channelNameGenerator)
        let server = new Server(listener)
        do shortWait ()

        server.SendMessage(ChannelName + "1", "heyhey")

        for msg in MESSAGES do
            server.SendMessage(ChannelName, msg)
        do shortWait ()

        for channel in listener.OpenedChannels do
            let history = channel.MessageHistory
            let sendMsgSet = Set.ofSeq MESSAGES
            let receivedMsgSet = Set.ofSeq history

            Assert.AreEqual(3, history.Length)
            Assert.AreEqual(sendMsgSet, receivedMsgSet)

//    [<TestMethod>]
//    member this.MultipleMessages_ToSameConnection_AreSerialized() =
//        Assert.IsTrue()