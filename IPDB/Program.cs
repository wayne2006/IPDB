using System;

namespace IPDB
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("----------------------------------------\r\n请选择：\r\n 1：查询IP\r\n 2：生成ip.db\r\n 3：退出");
                var input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        Console.WriteLine("输入需查询IP：");
                        var ip = Console.ReadLine();
                        var _search = IPSearch3Fast.Instance;
                        var result = _search.Find(ip);
                        Console.WriteLine($"[{result}]");

                        break;
                    case "2":
                        Console.WriteLine("开始生成");
                        string dstDir = "./data/";
                        if (!dstDir.EndsWith("/"))
                        {
                            dstDir = dstDir + "/";
                        }
                        try
                        {
                            DbMaker dbMaker = new DbMaker();
                            dbMaker.Make(dstDir + "ip.db");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                        break;
                    case "3":
                        Environment.Exit(0);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
