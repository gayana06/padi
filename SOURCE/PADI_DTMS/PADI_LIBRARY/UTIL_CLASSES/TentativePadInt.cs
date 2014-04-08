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

        #endregion
    }
}
