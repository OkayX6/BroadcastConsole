module BroadcastConsole.Common.Helpers

open System

let toAsync action =
    async {
        let context = System.Threading.SynchronizationContext.Current
        do! Async.SwitchToThreadPool()
        
        let result = action ()

        do! Async.SwitchToContext(context)
        return result
    }

let toAsyncWithContext context action =
    async {
        let oldContext = System.Threading.SynchronizationContext.Current
        do! Async.SwitchToContext context
        let result = action ()
        do! Async.SwitchToContext oldContext
        return result
    }