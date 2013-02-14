namespace BroadcastConsole.Common.Interfaces

open System
open System.Collections.Generic
open System.ServiceModel

[<ServiceContract(Namespace = "http://OkayX6.BroadcastConsole.Subscribers",
                  SessionMode = SessionMode.Required,
                  CallbackContract = typeof<ISubscriberServerDuplexCallback>)>]
type ISubscriberServer =
    [<OperationContract()>]
    abstract member SubscribeTo : channel: string -> unit

and ISubscriberServerDuplexCallback =
    [<OperationContract()>]
    abstract member Send : message: string -> unit


[<ServiceContract(Namespace = "http://OkayX6.BroadcastConsole.Publishers")>]
type IPublisherServer =
    [<OperationContract(IsOneWay = true)>]
    abstract member PublishTo : channel: string * message: string -> unit