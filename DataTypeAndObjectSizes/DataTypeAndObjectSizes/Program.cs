using System;

namespace DataTypeAndObjectSizes
{
    class Program
    {
        static void Main(string[] args)
        {
            // Compile with /platform:x64, see the 
            // project properties dialog in Visual Studio
            Console.WriteLine(sizeof(int));
            Console.WriteLine(System.Runtime.InteropServices.Marshal.SizeOf<int>());
        }
    }
}
