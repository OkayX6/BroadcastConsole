namespace BroadcastConsole.WcfService

open System
open System.Collections.Generic
open System.ServiceModel
open BroadcastConsole.Common.Interfaces


[<ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)>]
type Server() =
    let callbacks = new Dictionary<string, ISubscriberServerDuplexCallback list>()

    member this.Callback with
        get () = OperationContext.Current.GetCallbackChannel<ISubscriberServerDuplexCallback>()

    interface ISubscriberServer with
        member this.SubscribeTo(channel: string) : unit =
            ()
//            let callback = this.Callback
//
//            match callbacks.TryGetValue(channel) with
//            | true, callbackList -> callbacks.[channel] <- callback :: callbackList
//            | false, _           -> callbacks.[channel] <- [callback]