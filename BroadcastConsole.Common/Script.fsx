// This file is a script that can be executed with the F# Interactive.  
// It can be used to explore and test the library project.
// Note that script files will not be part of the project build.

#load "Helpers.fs"
#load "Interfaces.fs"
#load "Server.fs"

open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces

let makeChannel (channelName: string) = {
    new IChannel with
        member this.Receive () =
            channelName
        member this.Send (msg: string) =
            printfn "[%O] %O"
                System.Threading.Thread.CurrentThread.ManagedThreadId
                msg
    }

let channelListener = {
    new IChannelListener with
        member this.Accept () =
            makeChannel "Canal 1"
    }

let server = new Server(channelListener)

for i in 0 .. 20 do
    server.SendMessage("Canal 1", "hello")

