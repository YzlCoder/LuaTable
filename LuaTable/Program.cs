using System;

namespace LuaTable
{
    class Program
    {
        static void Main(string[] args)
        {
            LuaTable table = new LuaTable();
            for (int i = 50; i < 60; ++i)
            {
                table[i] = i * 10;
            }
            table[5] = null;

            foreach(var item in LuaTable.Pairs(table))
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("??");
            foreach (var item in LuaTable.Ipairs(table))
            {
                Console.WriteLine(item);
            }
        }
    }
}
