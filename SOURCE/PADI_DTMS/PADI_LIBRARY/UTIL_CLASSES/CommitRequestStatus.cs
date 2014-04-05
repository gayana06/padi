﻿#region Directive Section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace PADI_LIBRARY
{
    class CommitRequestStatus
    {
        #region Initialization
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

        private bool hasAborted;

        public bool HasAborted
        {
            get { return hasAborted; }
            set { hasAborted = value; }
        }

        #endregion
    }
}
