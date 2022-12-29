using ProtoBuf;

namespace Shared
{
    [ProtoContract]
    public class PlayerData
    {
        [ProtoMember(1)] public string Name;
    }    
}