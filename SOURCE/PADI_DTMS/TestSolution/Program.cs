using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestSolution
{
    class Program
    {
        static void Main(string[] args)
        {
           // DateTimeDiffCheck();
        }

        public static void DateTimeDiffCheck()
        {
            DateTime t = DateTime.Now;
            Thread.Sleep(3000);
            Console.WriteLine("Seconds ="+DateTime.Now.Subtract(t).Seconds);
            Console.WriteLine("Milliseconds ="+DateTime.Now.Subtract(t).Milliseconds);
        }
    }
}
