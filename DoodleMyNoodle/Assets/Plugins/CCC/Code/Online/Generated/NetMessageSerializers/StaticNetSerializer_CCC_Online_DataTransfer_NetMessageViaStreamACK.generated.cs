// THIS CODE IS GENERATED
// DO NOT MODIFY IT

using System;
using System.Collections.Generic;
public static class StaticNetSerializer_CCC_Online_DataTransfer_NetMessageViaStreamACK
{
    public static int GetNetBitSize(ref CCC.Online.DataTransfer.NetMessageViaStreamACK obj)
    {
        int result = 0;
        result += StaticNetSerializer_System_UInt16.GetNetBitSize(ref obj.TransferId);
        return result;
    }

    public static void NetSerialize(ref CCC.Online.DataTransfer.NetMessageViaStreamACK obj, BitStreamWriter writer)
    {
        StaticNetSerializer_System_UInt16.NetSerialize(ref obj.TransferId, writer);
    }

    public static void NetDeserialize(ref CCC.Online.DataTransfer.NetMessageViaStreamACK obj, BitStreamReader reader)
    {
        StaticNetSerializer_System_UInt16.NetDeserialize(ref obj.TransferId, reader);
    }
}
