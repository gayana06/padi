using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_LIBRARY
{
    class CommitRequestStatus
    {
        private ObjectServer server;

        public ObjectServer Server
        {
            get { return server; }
            set { server = value; }
        }

        private bool vote;

        public bool Vote
        {
            get { return vote; }
            set { vote = value; }
        }

        private bool hasCommited;

        public bool HasCommited
        {
            get { return hasCommited; }
            set { hasCommited = value; }
        }

    }
}
