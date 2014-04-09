#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
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

        #endregion

        #region Public Members

        /// <summary>
        /// Read from the object server, value of the ServerPadInt.
        /// </summary>
        /// <returns></returns>
        public int Read()
        {
            int value;
            try
            {
                value= worker.Read(this.UID, PADI_Client.TransactionId);
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
                worker.Write(this.UID, PADI_Client.TransactionId, value);
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
