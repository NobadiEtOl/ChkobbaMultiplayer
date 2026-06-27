using Unity.Netcode;

public struct PlayerCustomData : INetworkSerializable
{
    public string PlayerName;
    public int CardBackIndex;
    public int BaseFaceIndex;
    public int HairIndex;
    public int EyesIndex;
    public int EyebrowsIndex;
    public int MouthIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            if (PlayerName == null) PlayerName = string.Empty;
        }

        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref CardBackIndex);
        serializer.SerializeValue(ref BaseFaceIndex);
        serializer.SerializeValue(ref HairIndex);
        serializer.SerializeValue(ref EyesIndex);
        serializer.SerializeValue(ref EyebrowsIndex);
        serializer.SerializeValue(ref MouthIndex);
    }
}
