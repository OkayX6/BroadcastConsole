// This file is a script that can be executed with the F# Interactive.  
// It can be used to explore and test the library project.
// By default script files are not be part of the project build.

// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r "bin/Debug/BroadcastConsole.Common.dll"
#load "Implementation.fs"

open BroadcastConsole.Common
open BroadcastConsole.Common.Desktop
open BroadcastConsole.Common.Interfaces


// Define your library scripting code here
let tcpListener = new TcpConnectionListener(2009)
let server = new Server(tcpListener)

do
    while true do
        let ChannelName = "Cloud"
        let Msg = System.DateTime.Now.ToLongTimeString()

        printfn "[%O]: %O" ChannelName Msg

        server.SendMessage(ChannelName, Msg)
        System.Threading.Thread.Sleep(millisecondsTimeout = 1000)
