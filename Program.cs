﻿using System;

namespace N1T1Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                Tool tool = new Tool();
                tool.SendInitrd(args[0]);
            }
        }
    }
}
