using System.Collections.Generic;

namespace StayNet.Common.Entities
{
    internal static class ExtensionMethods
    {

        public static void Print(this byte[] array)
        {
            Console.WriteLine("---");
            for (int i = 0; i < array.Length; i++)
            {
                Console.Write(array[i]);
            }
            Console.WriteLine("\n---");
        }
        
        public static void Print(this List<byte> array)
        {
            Print(array.ToArray());            
        }
        
    }
}