namespace shared
{
    /**
     * Classes that implement the ISerializable interface can (de)serialize themselves
     * into/out of a Packet instance. See the protocol package for an example.
     */
    public interface ISerializable
    {
        /**
         * Write all the data for 'this' object into the Packet
         */
        void Serialize(Packet pPacket);
        
        /**
         * Read all the data for 'this' object from the Packet
         */
        void Deserialize(Packet pPacket);
    }
}
