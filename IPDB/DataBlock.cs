using System;
using System.Text;

namespace IPDB
{
    public class DataBlock
    {
        /**
         * region address
        */
        public string region;

        /**
         * region ptr in the db file
        */
        public int dataPtr;

        public DataBlock(string _region, int _dataPtr)
        {
            this.region = _region;
            this.dataPtr = _dataPtr;
        }

        public DataBlock(string region) : this(region, 0)
        {
        }


        public string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(region).Append('|').Append(dataPtr);
            return sb.ToString();
        }

    }
}
