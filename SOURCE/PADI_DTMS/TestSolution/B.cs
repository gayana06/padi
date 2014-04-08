using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestSolution
{
    class B
    {
        A a;
        C c;
        public B(A a)
        {
            c = new C(a);
            this.a = a;
        }
        public void TestB()
        {

                Console.WriteLine("Run B");
                Console.WriteLine("Thread name" + Thread.CurrentThread.ManagedThreadId);
                //Monitor.Wait(this);
                c.TestC();
            
        }
    }
}
