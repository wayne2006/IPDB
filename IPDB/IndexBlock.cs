using System;
namespace IPDB
{

    public class IndexBlock
    {
        public static int LENGTH = 12;

        /**
         * start ip address 
        */
        public long startIp;

        /**
         * end ip address 
        */
        public long endIp;

        /**
         * data ptr and data length 
        */
        public int dataPtr;

        /**
         * data length 
        */
        public int dataLen;

        public IndexBlock(long startIp, long endIp, int dataPtr, int dataLen)
        {
            this.startIp = startIp;
            this.endIp = endIp;
            this.dataPtr = dataPtr;
            this.dataLen = dataLen;
        }

        /**
         * get the bytes for storage
         * 
         * @return    byte[]
        */
        public byte[] ToBytes()
        {
            /*
             * +------------+-----------+-----------+
             * | 4bytes        | 4bytes    | 4bytes    |
             * +------------+-----------+-----------+
             *  start ip      end ip      data ptr + len 
            */
            byte[] b = new byte[12];

            Utils.WriteIntLong(b, 0, startIp);    //start ip
            Utils.WriteIntLong(b, 4, endIp);        //end ip

            //write the data ptr and the length
            long mix = dataPtr | ((dataLen << 24) & 0xFF000000L);
            Utils.WriteIntLong(b, 8, mix);

            return b;
        }
    }
}
