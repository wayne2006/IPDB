using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IPDB
{


    /**
     * fast ip db maker
     * 
     * db struct:
     * 1: index size part:
     * +------------+
     * | 4 bytes	|
     * +------------+
     *  total recode number
     * 
     * 2: prefix part
     * +------------+-----------+
     * | 4bytes		| 4bytes	| 
     * +------------+-----------+
     *  start recode index ,end recode index
     *  
     * 3. index part:(ip range)
     * +------------+-----------+---------------+
     * | 4bytes		| 4bytes	| 4bytes		|
     * +------------+-----------+---------------+
     *  start ip 	  end ip	  3 byte data ptr & 1 byte data length
     *  
     * 4. data part: 
     * +-----------------------+
     * | dynamic length		   |
     * +-----------------------+
     *  data length   Country|Area|Province|City|ISP 例：中国|华南|广东|深圳|电信
     * 
*/
    public class DbMaker
    {
        /**
         * ip source file path
        */
        private FileInfo ipSrcFile;
        private string ipSrcFilePath;
        private Hashtable ht = new Hashtable();

        /**
         * buffer 
        */
        private List<IndexBlock> indexPool;
        private List<PrefixBlock> prefixBlockPool;

        /**
         * region and data ptr mapping data 
        */
        private Dictionary<string, DataBlock> regionPtrPool = null;

        /**
         * construct method
         * 
         * @param	config
         * @param	ipSrcFile tb source ip file
         * @param	globalRegionFile global_region.csv file offer by lion
         * @throws	DbMakerConfigException 
         * @throws	IOException 
        */
        public DbMaker()
        {
            ipSrcFilePath = @"./data/ip.merge.txt";
            this.ipSrcFile = new FileInfo(ipSrcFilePath);
            this.regionPtrPool = new Dictionary<string, DataBlock>();

            if (this.ipSrcFile.Exists == false)
            {
                throw new Exception("Error: Invalid file path " + ipSrcFile);
            }
        }

        /**
         * initialize the db file 
         * 
         * @param	raf
         * @ 
        */
        private void initDbFile(FileStream raf, int indexlength)
        {
            //1. zero fill the header part
            raf.Seek(0L, SeekOrigin.Begin);
            raf.Write(new byte[4]);     //indexSize block
            raf.Write(new byte[256 * 8]);     //prefix block
            raf.Write(new byte[indexlength * 12]);     //index block

            prefixBlockPool = new List<PrefixBlock>();
            indexPool = new List<IndexBlock>();
        }


        public void Make(string dbFile)
        {

            FileStream raf = new FileStream(dbFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            string[] ipSrcFileArray = System.IO.File.ReadAllLines(ipSrcFilePath);

            //init the db file
            initDbFile(raf, ipSrcFileArray.Length);
            Console.WriteLine("+-Db file initialized.");

            //analysis main loop
            Console.WriteLine("+-Try to write the data blocks ... ");
            //String line = null;
            foreach (var item in ipSrcFileArray)
            {
                var line = item.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith('#')) continue;

                //1. get the start ip
                int sIdx = 0, eIdx = 0;
                if ((eIdx = line.IndexOf('|', sIdx + 1)) == -1) continue;
                String startIp = line.Substring(sIdx, eIdx - sIdx);

                //2. get the end ip
                sIdx = eIdx + 1;
                if ((eIdx = line.IndexOf('|', sIdx + 1)) == -1) continue;
                String endIp = line.Substring(sIdx, eIdx - sIdx);

                //3. get the region
                sIdx = eIdx + 1;
                String region = line.Substring(sIdx);

                //Console.WriteLine("+-Try to process item " + line);
                AddDataBlock(raf, startIp, endIp, region);
                //Console.WriteLine("|--[Ok]");
            }

            Console.WriteLine("|--Data block flushed!");
            Console.WriteLine("|--Data file pointer: " + raf.Position + "\n");

            //write the index bytes
            Console.WriteLine("+-Try to write index blocks ... ");

            //record the start block
            IndexBlock indexBlock = null;

            //int blockLength = IndexBlock.LENGTH;//12
            int counter = 0;

            long indexStratPtr, indexPtr;
            int ipPrefix = 0;
            int startNum = 0, endNum = 0;
            raf.Seek(2052L,SeekOrigin.Begin);
            foreach (var indexIt in indexPool)
            {
                
                indexBlock = indexIt;

                endNum = counter;
                //indexPtr = raf.getFilePointer();
                int ipPrefix_new = int.Parse(Utils.Long2ip(indexBlock.startIp).Split('.')[0]);
                
                if (ipPrefix != ipPrefix_new)
                {
                    Console.WriteLine($"ipPrefix:{ipPrefix} ipPrefix_new:{ipPrefix_new}");
                    //判断IP前缀是否重复
                    if (ht.ContainsKey(ipPrefix))
                    {
                        throw new Exception("IP前缀重复");
                    }
                    ht.Add(ipPrefix, null);

                    var pb = new PrefixBlock(ipPrefix, startNum, endNum-1);
                    prefixBlockPool.Add(pb);
                    ipPrefix = ipPrefix_new;
                    startNum = endNum ;
                }
                //write the buffer
                raf.Write(indexBlock.ToBytes());
                counter++;
            }

            //写入最后一个prefix block
            var pblast = new PrefixBlock(ipPrefix, startNum, endNum);
            prefixBlockPool.Add(pblast);

            //write prefix blocks
            raf.Seek(4L,SeekOrigin.Begin);
            for (int i = 0; i < 256; i++)
            {
                if (i < prefixBlockPool.Count)
                {
                    var prefixBlock = prefixBlockPool[i];
                    raf.Write(prefixBlock.ToBytes());
                }
                else
                {
                    raf.Write(new byte[8]);
                }
            }

            Console.WriteLine("|--[Ok]");

            //write the super blocks
            Console.WriteLine("+-Try to write the super blocks ... ");
            raf.Seek(0L,SeekOrigin.Begin);   //reset the file pointer

            byte[] indexSize = new byte[4];
            Utils.WriteIntLong(indexSize, 0, indexPool.Count);
            raf.Write(indexSize);
            Console.WriteLine("|--[Ok]");

            //write the header blocks
            Console.WriteLine("+-Try to write the header blocks ... ");

            //write the copyright and the release timestamp info
            Console.WriteLine("+-Try to write the copyright and release date info ... ");
            raf.Seek(raf.Length,SeekOrigin.Begin);

            string copyright = "Created by www at " + DateTime.Now.ToString("yyyy/MM/dd"); 
            var timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            raf.Write(BitConverter.GetBytes(timestamp));   //the unix timestamp
            raf.Write(Encoding.UTF8.GetBytes(copyright));
            Console.WriteLine("|--[Ok]");

            raf.Close();
        }


        /**
         * internal method to add a new data block record
         * 
         * @param	raf
         * @param	startIp
         * @param	endIp
         * @param	region data
        */
        private void AddDataBlock(FileStream raf, string startIp, string endIp, string region)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(region);
                int dataPtr = 0;

                //check region ptr pool first
                if (regionPtrPool.ContainsKey(region))
                {
                    DataBlock dataBlock = regionPtrPool[region];
                    dataPtr = dataBlock.dataPtr;
                    //Console.WriteLine("dataPtr: " + dataPtr + ", region: " + region);
                }
                else
                {
                    dataPtr = (int)raf.Position;
                    raf.Write(data);

                    regionPtrPool.Add(region, new DataBlock(region, dataPtr));
                }

                //add the data index blocks
                IndexBlock ib = new IndexBlock(
                    Utils.Ip2long(startIp),
                    Utils.Ip2long(endIp),
                    dataPtr,
                    data.Length
                );
                indexPool.Add(ib);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
