namespace BroadcastConsole.Common.Interfaces

type IConnection =
    abstract member Receive : unit -> string
    abstract member Send : string -> unit

type IConnectionListener =
    abstract member Accept : unit -> IConnection