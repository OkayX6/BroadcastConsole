module BroadcastConsole.Common.Helpers

open System

let internal toAsync action =
    async {
        let context = System.Threading.SynchronizationContext.Current
        do! Async.SwitchToThreadPool()
        
        let result = action ()

        do! Async.SwitchToContext(context)
        return result
    }

let internal toAsyncWithContext context action =
    async {
        let oldContext = System.Threading.SynchronizationContext.Current
        do! Async.SwitchToContext context
        let result = action ()
        do! Async.SwitchToContext oldContext
        return result
    }

/// Converts an int to a byte array (big-endian)
let intToByteArray (i: int) =
    BitConverter.GetBytes(i)

/// Converts the 4 first bytes of a byte array to an int (big-endian) 
let byteArrayToInt (bytes: byte[]) =
    BitConverter.ToInt32(bytes, 0)

/// Converts a string to a byte array (with UTF8 encoding)
let stringToByteArray (s: string) =
    System.Text.Encoding.UTF8.GetBytes(s)

/// Converts a byte array as a string (UTF8 decoding)
let byteArrayToString (bytes: byte[]) =
    System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length)

/// Packet header
let PacketHeaderLength = 4

/// Converts a string to a byte array packet
let stringToPacket (s: string) =
    let stringByteArray = s |> stringToByteArray
    let packetHeader = stringByteArray.Length |> intToByteArray
    let packetSize = packetHeader.Length + stringByteArray.Length
    let packet = Array.zeroCreate packetSize

    Array.blit
        packetHeader 0
        packet 0
        PacketHeaderLength
    
    Array.blit
        stringByteArray 0
        packet PacketHeaderLength
        stringByteArray.Length

    packet

/// Converts a byte array packet to a string
let packetToString (packet: byte[]) =
    packet.[4 .. packet.Length - 1] |> byteArrayToString