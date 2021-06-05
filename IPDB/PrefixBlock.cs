using System;
namespace IPDB
{

    public class PrefixBlock
    {
        /// <summary>
        /// ip前缀
        /// </summary>
        private int prefix;
        /// <summary>
        /// 相同前缀的IP段的开始行数
        /// </summary>
        private long indexStart;

        /// <summary>
        /// 相同前缀的IP段的结束行数
        /// </summary>
        private long indexEnd;

        public PrefixBlock(int _prefix,long _indexStart, long _indexEnd)
        {
            prefix = _prefix;
            indexStart = _indexStart;
            indexEnd = _indexEnd;
        }

        public byte[] ToBytes()
        {
            /*
             * +------------+-----------+
             * | 4bytes        | 4bytes    |
             * +------------+-----------+
             *  start index      end index 
            */
            byte[] b = new byte[8];

            Utils.WriteIntLong(b, 0, indexStart);
            Utils.WriteIntLong(b, 4, indexEnd);

            return b;
        }
    }
}
