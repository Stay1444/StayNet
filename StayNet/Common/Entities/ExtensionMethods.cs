using System.Collections.Generic;

namespace StayNet.Common.Entities
{
    internal static class ExtensionMethods
    {

        public static bool Print(this byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Console.Write(array[i]);
            }
            Console.WriteLine();
            return true;
        }

        public static bool Print(this object @object)
        {
            Console.WriteLine(@object.ToString());
            return true;
        }
        
        public static void Print(this List<byte> array)
        {
            Print(array.ToArray());            
        }
        
    }
}