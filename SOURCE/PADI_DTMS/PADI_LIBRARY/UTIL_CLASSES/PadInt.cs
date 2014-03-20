using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY.UTIL_CLASSES
{
    public class PadInt
    {
        private int uid;

        public int Uid
        {
            get { return uid; }
            set { uid = value; }
        }

        private int value;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        public PadInt(int uid, int value)
        {
            this.uid = uid;
            this.value = value;
        }

    }
}
