using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestSolution
{
    public class A
    {
        B b;
        public A()
        {
            b = new B(this);

        }

        public void TestA()
        {
            Console.WriteLine("Thread id = "+Thread.CurrentThread.ManagedThreadId);
            lock (this)
            {
                Console.WriteLine("Run A");
                Console.WriteLine("Thread name" + Thread.CurrentThread.ManagedThreadId);
                // Monitor.Wait(this);
                b.TestB();
            }
        }
    }
}
