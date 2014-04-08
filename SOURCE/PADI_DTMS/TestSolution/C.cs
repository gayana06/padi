using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestSolution
{
    class C
    {
        A a;
        bool b = false;
        public C(A a)
        {
            this.a = a;
        }

        public void TestC()
        {

                Console.WriteLine("Waiting C");
                Console.WriteLine("Thread name" + Thread.CurrentThread.ManagedThreadId);
                if (!b)
                {
                    b = true;
                    Monitor.Wait(a);
                }
                else
                {
                    Monitor.PulseAll(a);
                }
            
        }
    }
}
