namespace BroadcastConsole.Common.Interfaces

open System

type IConnection =
    inherit IDisposable
    
    abstract member Receive : unit -> string
    abstract member Send : string -> unit

type IConnectionListener =
    abstract member Accept : unit -> IConnection