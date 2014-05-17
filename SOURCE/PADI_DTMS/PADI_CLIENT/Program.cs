using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PADI_LIBRARY;
using System.Windows.Forms;
using System.Threading;

namespace PADI_CLIENT
{
    class Program
    {
        static void Main(string[] args)
        {

            Common.Logger().LogInfo("Client started", string.Empty, string.Empty);
            // ClientForm f = new ClientForm();
            // Application.Run(f);
            // Client c = new Client();
            //  c.Start();

            try
            {
                bool res;

                PADI_Client.Init();
                //  res = PADI_Client.TxBegin();
                //  PADI_Client.Status();
                //  PADI_Client.TxAbort();
                //  Console.ReadLine();
                // res = PADI_Client.Fail("tcp://127.0.0.1:25051/PADI_Worker");
                // res = PADI_Client.Recover("tcp://127.0.0.1:25051/PADI_Worker");
                // Thread.Sleep(5000);

                /*    res = PADI_Client.TxBegin();
                     PadInt pi_a = PADI_Client.CreatePadInt(0);
                     PadInt pi_b = PADI_Client.CreatePadInt(1);
                     PadInt pi_c = PADI_Client.CreatePadInt(2);
                      pi_a.Write(33);
                      pi_b.Write(34);
                      pi_c.Write(35);
                     res = PADI_Client.TxCommit();
                     PADI_Client.Status(); */


                res = PADI_Client.TxBegin();
                PadInt pi_a = PADI_Client.AccessPadInt(0);
                if (pi_a == null)
                    pi_a = PADI_Client.CreatePadInt(0);
                PadInt pi_b = PADI_Client.AccessPadInt(1);
                if (pi_b == null)
                    pi_b = PADI_Client.CreatePadInt(1);
                PadInt pi_c = PADI_Client.AccessPadInt(2);
                if (pi_c == null)
                    pi_c = PADI_Client.CreatePadInt(2);
                pi_a.Write(101);
                pi_b.Write(102);
                pi_c.Write(103);
                res = PADI_Client.TxCommit();
                PADI_Client.Status();

                Console.WriteLine("Press enter to check timeout");
                Console.ReadLine();

                res = PADI_Client.TxBegin();
                pi_c = PADI_Client.AccessPadInt(2);
                pi_c.Write(500);
                Console.WriteLine("Checking whether this will automatically timeout");
                Console.ReadLine();
                res = PADI_Client.TxCommit();
                PADI_Client.Status();

                Console.ReadLine();

                int index = 1500;
                while (true)
                {
                    try
                    {
                        res = PADI_Client.TxBegin();
                        pi_a = PADI_Client.AccessPadInt(0);
                        pi_b = PADI_Client.AccessPadInt(1);
                        pi_c = PADI_Client.AccessPadInt(2);
                        if (index % 6 == 0)
                        {
                            Console.WriteLine(pi_a.Read() + ", " + pi_b.Read() + ", " + pi_c.Read());
                        }
                        Thread.Sleep(500);
                        pi_a.Write(index);
                        pi_b.Write(index * 3);
                        pi_c.Write(index * 5);
                        if (index == 1700)
                        {
                            Console.WriteLine("Enter to finish....");
                            Console.ReadLine();
                            PADI_Client.TxCommit();
                            break;
                        }
                        index++;
                        PADI_Client.TxCommit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                PADI_Client.Status();




                /*     res = PADI_Client.TxBegin();
                     pi_a = PADI_Client.CreatePadInt(4);
                     pi_a.Write(200);
                     PADI_Client.Status();
                     Console.ReadLine();
                     pi_a = PADI_Client.CreatePadInt(5);
                     pi_a.Write(300);                   
                     res = PADI_Client.TxCommit();
                     PADI_Client.Status(); 

                     Console.ReadLine();  */

                /*    PADI_Client.TxBegin();
                    PadInt a = PADI_Client.AccessPadInt(5);
                    Console.WriteLine(a.Read());

                    PadInt b = PADI_Client.AccessPadInt(3);
                    Console.WriteLine(b.Read());
                    PADI_Client.TxCommit();
                    PADI_Client.Status(); */

                /*  res = PADI_Client.TxBegin();
                  PADI_Client.Freeze("tcp://localhost:25051/PADI_Worker");
                  pi_a = PADI_Client.AccessPadInt(0);
                  pi_b = PADI_Client.AccessPadInt(1);
                  pi_a.Write(36);
                  pi_b.Write(37);
                  Console.WriteLine("a = " + pi_a.Read());
                  Console.WriteLine("b = " + pi_b.Read());
                  res = PADI_Client.Recover("tcp://localhost:25051/PADI_Worker");
                  PADI_Client.Status();
                  // The following 3 lines assume we have 2 servers: one at port 2001 and another at port 2002
                  // res = PADI_Client.Freeze("tcp://localhost:25052/PADI_Worker");
                  // res = PADI_Client.Recover("tcp://localhost:25052/PADI_Worker");
               
                  res = PADI_Client.TxCommit();*/


            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Transaction aborted : " + PADI_Client.TxAbort());
                PADI_Client.Status();
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
                //Console.WriteLine("Transaction aborted : " + PADI_Client.TxAbort());
                // PADI_Client.TxAbort();
            }
            finally
            {
                Console.WriteLine("-----------Client execution ended----------");
                Console.ReadLine();
            }
        }
    }
}
