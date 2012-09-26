namespace BroadcastConsole.Test

open System
open System.Threading
open Microsoft.VisualStudio.TestTools.UnitTesting
open BroadcastConsole.Common
open BroadcastConsole.Common.Interfaces
open BroadcastConsole.Common.Helpers
open BroadcastConsole.Test.Mocks

[<TestClass>]
type HelpersTests() =
    [<TestMethod>]
    member this.IntToByteArrayTest() =
        let (?=) (x: int) =
            x == (x |> intToByteArray |> byteArrayToInt)

        (?=) 0
        (?=) -1
        (?=) 1
        (?=) Int32.MaxValue
        (?=) Int32.MinValue

    [<TestMethod>]
    member this.StringToByteArrayTest() =
        let (?=) (x: string) =
            x == (x |> stringToByteArray |> byteArrayToString)

        (?=) String.Empty
        (?=) ""
        (?=) "a"
        (?=) "hello world"
        (?=) "   "
        (?=) " a "

    [<TestMethod>]
    member this.StringToPacketTest() =
        let s1 = String.Empty
        let s2 = ""
        let s3 = "a"
        let s4 = "hello world"
        let s5 = "   "
        let s6 = " a "

        let identity = stringToPacket >> packetToString

        s1 == (identity s1)
        s2 == (identity s2)
        s3 == (identity s3)
        s4 == (identity s4)
        s5 == (identity s5)
        s6 == (identity s6)