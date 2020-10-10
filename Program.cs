using System;

namespace N1T1Tool
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                Tool tool = new Tool();
                return (int)tool.SendInitrd(args[0]);
            }
            else
            {
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " [initrd-file]");
                return (int)StatusCode.Success;
            }
        }
    }
}
