﻿using PADI_LIBRARY;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_CLIENT
{
    class Client
    {

        private  const string APP_SET_TASK = "TASK";
        private const string APP_SET_SLEEP_TIME = "SLEEP_TIME";
        private const string BEGIN_TRANSACTION = "BT";
        private const string END_TRANSACTION = "ET";
        private const string CREATE_PADINT = "CPI";
        private const string ACCESS_PADINT = "API";
        private const string READ = "RD";
        private const string WRITE = "WT";
        private const string STATUS_DUMP = "STD";
        private const string FREEZE = "FZ";
        private const string FAIL = "FL";
        private const string RECOVER = "REC";
        private const char SEP_CHAR_COMMA = ',';
        private const string SEP_STR_COMMA = ",";
        private const char SEP_CHAR_HYPHEN = '-';
        private const string SEP_STR_COLON = ":";
        

        private string[] operationArray;


        public Client()
        {
            PADI_Client.Init();
        }

        public void Start()
        {
            try
            {
                for (int i = 0; i < Int32.Parse(ConfigurationManager.AppSettings[APP_SET_SLEEP_TIME]); i++)
                {
                    Console.WriteLine("Starts : " + (Int32.Parse(ConfigurationManager.AppSettings[APP_SET_SLEEP_TIME]) - i));
                    Thread.Sleep(1000);
                }
                operationArray = ConfigurationManager.AppSettings[APP_SET_TASK].Split(SEP_CHAR_COMMA);
                string[] tmp;
                PadInt padInt = null;
                foreach (var operation in operationArray)
                {
                    tmp = operation.Split(SEP_CHAR_HYPHEN);
                    bool status = false;
                    switch (tmp[0])
                    {
                        case BEGIN_TRANSACTION:
                            status = PADI_Client.TxBegin();
                            Console.WriteLine("Transaction started. " + status);
                            break;
                        case END_TRANSACTION:
                            status = PADI_Client.TxCommit();
                            Console.WriteLine("Transaction committed. " + status);
                            break;
                        case CREATE_PADINT:
                            padInt = PADI_Client.CreatePadInt(Int32.Parse(tmp[1]));
                            break;
                        case ACCESS_PADINT:
                            padInt = PADI_Client.AccessPadInt(Int32.Parse(tmp[1]));
                            break;
                        case READ:
                            if (padInt != null)
                                Console.WriteLine("Read value = " + padInt.Read());
                            else
                                Console.WriteLine("PadInt is null - READ");
                            break;
                        case WRITE:
                            if (padInt != null)
                            {
                                padInt.Write(Int32.Parse(tmp[1]));
                                Console.WriteLine("Write issued = " + tmp[1]);
                            }
                            else
                                Console.WriteLine("PadInt is null - WRITE");
                            break;
                        case STATUS_DUMP:
                            PADI_Client.Status();
                            Console.WriteLine("Dumped Status");
                            break;
                        case FREEZE:
                            PADI_Client.Freeze(tmp[1]);
                            break;
                        case FAIL:
                            PADI_Client.Fail(tmp[1]);
                            break;
                        case RECOVER:
                            PADI_Client.Recover(tmp[1]);
                            Thread.Sleep(5000); //wait until it stables
                            break;
                        default:
                            Console.WriteLine("Invalid input");
                            break;
                    }
                }
            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Transaction aborted : " + PADI_Client.TxAbort());
                PADI_Client.Status();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Transaction aborted : " + PADI_Client.TxAbort());
                PADI_Client.TxAbort();
            }
            finally
            {
                Console.WriteLine("-----------Client execution ended----------");
                Console.ReadLine();
            }

        }

    }

}
