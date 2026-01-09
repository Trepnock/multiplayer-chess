using System.Reflection.Emit;
using Unity.Collections;
using Unity.Networking.Transport;



public class NetMessages
{
    public OpCode Code {  get; set; }

    public virtual void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
    }
    public virtual void Deserialize(DataStreamReader reader)
    {

    }
    public virtual void ReceivedOnClient()
    {

    }
    public virtual void ReceivedOnServer(NetworkConnection cnn) //need to know who sent the packet
    {
        
    }

}
