using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    class FreezedOperation
    {
        private int index;

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        private string operation;

        public string Operation
        {
            get { return operation; }
            set { operation = value; }
        }

        private long tid;

        public long Tid
        {
            get { return tid; }
            set { tid = value; }
        }

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

        private bool isReleased;

        public bool IsReleased
        {
            get { return isReleased; }
            set { isReleased = value; }
        }

        private string result;

        public string Result
        {
            get { return result; }
            set { result = value; }
        }

    }
}
