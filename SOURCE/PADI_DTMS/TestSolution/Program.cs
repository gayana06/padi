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
            CheckTicks();
        }

        public static void CheckTicks()
        {
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("Ticks : " +DateTime.Now.Ticks);
                Thread.Sleep(1);
            }
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
