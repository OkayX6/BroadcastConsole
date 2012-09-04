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
        let i1 = 0
        let i2 = -1
        let i3 = 1
        let i4 = Int32.MaxValue
        let i5 = Int32.MinValue

        let identity = intToByteArray >> byteArrayToInt

        i1 == (identity i1)
        i2 == (identity i2)
        i3 == (identity i3)
        i4 == (identity i4)
        i5 == (identity i5)

    [<TestMethod>]
    member this.StringToByteArrayTest() =
        let s1 = String.Empty
        let s2 = ""
        let s3 = "a"
        let s4 = "hello world"
        let s5 = "   "
        let s6 = " a "

        let identity = stringToByteArray >> byteArrayToString

        s1 == (identity s1)
        s2 == (identity s2)
        s3 == (identity s3)
        s4 == (identity s4)
        s5 == (identity s5)
        s6 == (identity s6)

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