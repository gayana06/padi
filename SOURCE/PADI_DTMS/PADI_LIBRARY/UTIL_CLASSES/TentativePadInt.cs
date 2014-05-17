#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace PADI_LIBRARY
{
    [Serializable]
    public class TentativePadInt
    {
        #region Initialization
     
        public TentativePadInt(long writeTS,int value)
        {
            this.WriteTS=writeTS;
            this.Value=value;
            CreatedTimeStamp = DateTime.Now;
        }

        private long writeTS;

        public long WriteTS
        {
            get { return writeTS; }
            set { writeTS = value; }
        }

        private int value;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        private DateTime createdTimeStamp;

        public DateTime CreatedTimeStamp
        {
            get { return createdTimeStamp; }
            set { createdTimeStamp = value; }
        }

        #endregion
    }
}
