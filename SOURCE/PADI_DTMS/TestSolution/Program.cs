using PADI_LIBRARY;
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
            Program p = new Program();
           // DateTimeDiffCheck();
           // CheckTicks();
           // p.Call();
            p.TestException();
        }

        public void TestException()
        {
            try
            {
                throw new TxException("This is test exception");
            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message+"---------General");
                Console.WriteLine(ex.StackTrace + "---------General");
            }

        }


        int WID = 4;
        int TID = 0;
        public void Call()
        {
            Thread t=new Thread(new ThreadStart(Releaser));
            t.Start();
            for (int i = 0; i < 4; i++)
            {                
                Thread k = new Thread(new ParameterizedThreadStart(Read));                
                k.Start(i);               
            }
               
            
        }

        public void Read(Object k)
        {
            lock (this)
            {
                int val = -1;
                while (true)
                {                    
                    if (TID < WID)
                    {
                        Monitor.Wait(this);

                    }
                    else
                    {
                        val = 10;
                        Console.WriteLine("Thread id = "+k +" Val="+val);
                       if(k.ToString()!="2")
                         WID = 4;
                        break;
                    }
                }
                //return val;
            }
        }

        public void Releaser()
        {
            for (int i = 0; i < 4; i++)
            {
                Thread.Sleep(1000);
                WID = -1;
                lock (this)
                {
                    Monitor.PulseAll(this);
                }
            }
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
