#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

#endregion

namespace PADI_LIBRARY
{
    public class PadInt
    {
        #region Initialization

        int uID;

        public int UID
        {
            get { return uID; }
            set { uID = value; }
        }

        PADI_Worker worker;

        public PADI_Worker Worker
        {
            get { return worker; }
            set { worker = value; }
        }

        PADI_Client client;

        public PadInt(int uID)
        {
            this.UID = uID;
        }

        public delegate int AsyncReadPadInt(int UID, long TID);
        public delegate void AsyncWritePadInt(int UID,long TID,int value);

        #endregion

        #region Public Members

        /// <summary>
        /// Read request will be replied here
        /// </summary>
        /// <param name="ar"></param>
        public  void AsyncReadCallBack(IAsyncResult ar)
        {
            try
            {
                AsyncReadPadInt del = (AsyncReadPadInt)((AsyncResult)ar).AsyncDelegate;
                Console.WriteLine("Read the value " + del.EndInvoke(ar));
            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }
        

        /// <summary>
        /// Write request will be requested here
        /// </summary>
        /// <param name="ar"></param>
        public  void AsyncWriteCallBack(IAsyncResult ar)
        {
            try
            {
                AsyncWritePadInt del = (AsyncWritePadInt)((AsyncResult)ar).AsyncDelegate;
                Console.WriteLine("Write successful");
            }
            catch (TxException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Read from the object server, value of the ServerPadInt.
        /// </summary>
        /// <returns></returns>
        public int Read()
        {
            int value;
            try
            {
                if (PADI_Client.TransactionId != 0)
                {
                    if (worker.IsThisServerFreezed)
                    {
                        AsyncReadPadInt read = new AsyncReadPadInt(worker.Read);
                        AsyncCallback readCallback = new AsyncCallback(AsyncReadCallBack);
                        IAsyncResult RemAr = read.BeginInvoke(this.UID, PADI_Client.TransactionId, readCallback, null);
                        Console.WriteLine("Read will be delayed due to server freeze, this is a junk value");
                        value= Int32.MinValue;
                    }
                    else
                    {
                        value = worker.Read(this.UID, PADI_Client.TransactionId);
                    }
                }
                else
                {
                    throw new TxException("Transaction is required to be started before read operation");
                }
            }
            catch (TxException ex)
            {
                Common.Logger().LogError(ex.Message,ex.StackTrace,ex.Source);
                Console.WriteLine(ex.Message);
                throw ex;
            }
            return value;
        }

        /// <summary>
        /// Write the value to the ServerPadInt in the object server
        /// </summary>
        /// <param name="value"></param>
        public void  Write(int value)
        {
            try
            {
                if (PADI_Client.TransactionId != 0)
                {
                    if (worker.IsThisServerFreezed)
                    {
                        AsyncWritePadInt write = new AsyncWritePadInt(worker.Write);
                        AsyncCallback writeCallback = new AsyncCallback(AsyncWriteCallBack);
                        IAsyncResult RemAr = write.BeginInvoke(this.UID, PADI_Client.TransactionId, value, writeCallback, null);
                        Console.WriteLine("Write will be delayed due to server freeze.");
                    }
                    else
                    {
                        worker.Write(this.UID, PADI_Client.TransactionId, value);
                    }
                }
                else
                {
                    throw new TxException("Transaction is required to be started before write operation");
                }
            }
            catch (TxException ex)
            {
                Common.Logger().LogError(ex.Message, ex.StackTrace, ex.Source);
                throw ex;
            }
        }

        #endregion
    }
}
