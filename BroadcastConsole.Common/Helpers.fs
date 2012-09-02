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

/// Converts an int to a byte array (big-endian)
let byteArrayOfInt (i: int) =
    BitConverter.GetBytes(i)

/// Converts the 4 first bytes of a byte array to an int (big-endian) 
let intOfByteArray (bytes: byte[]) =
    BitConverter.ToInt32(bytes, 0)

/// Converts a string to a byte array (with UTF8 encoding)
let byteArrayOfString (s: string) =
    System.Text.Encoding.UTF8.GetBytes(s)

/// Converts a byte array as a string (UTF8 decoding)
let stringOfByteArray (bytes: byte[]) =
    System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length)