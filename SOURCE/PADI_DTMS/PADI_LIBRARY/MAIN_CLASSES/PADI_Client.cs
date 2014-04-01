﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class PADI_Client:  MarshalByRefObject
    {

        PADI_Coordinator coordinator;
        PADI_Master master;
        List<PADI_Worker> workers;
        Information info;
        long transactionId;
        Dictionary<int, PadInt> clientAccessedPadInts;

        public PADI_Client()
        {
            //TODO:Load the Info object when start and save it when client exit. Currently new object is created.
            coordinator = (PADI_Coordinator)Activator.GetObject(typeof(PADI_Coordinator), Common.GenerateTcpUrl(ConfigurationManager.AppSettings[Constants.APPSET_MASTER_IP], ConfigurationManager.AppSettings[Constants.APPSET_MASTER_PORT], Constants.OBJECT_TYPE_PADI_COORDINATOR));
            master = (PADI_Master)Activator.GetObject(typeof(PADI_Master), Common.GetMasterTcpUrl());
            workers = new List<PADI_Worker>();
            info = new Information();
            clientAccessedPadInts = new Dictionary<int, PadInt>();
        }

        /// <summary>
        /// This make the lease to expire never.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public PadInt CreatePadInt(int uid)
        {
            lock (this)
            {
                int modIndex = GetModuloServer(uid);
                PadInt padInt = null;
                if (modIndex >= 0)
                {
                    padInt = workers[modIndex].CreatePadInt(uid);
                    clientAccessedPadInts.Add(uid, padInt);
                }
                else
                    Console.WriteLine("No object servers found");
                return padInt;
            }
        }

        public PadInt AccessPadInt(int uid)
        {
            lock (this)
            {
                int modIndex = GetModuloServer(uid);
                PadInt padInt = null;
                if (modIndex >= 0)
                {
                    padInt = workers[modIndex].AccessPadInt(uid);
                    clientAccessedPadInts.Add(uid, padInt);
                }
                else
                    Console.WriteLine("No object servers found");
                return padInt;
            }
        }

        public int Read(int uid)
        {
            PadInt padInt = clientAccessedPadInts[uid];
            return padInt.Read(transactionId);
        }

        public void Write(int uid, int value)
        {
            PadInt padInt = clientAccessedPadInts[uid];
            padInt.Write(transactionId,value);
        }

        /// <summary>
        /// Set the TID from coordinator and load server map if the available map is expired
        /// </summary>
        /// <returns></returns>
        public bool BeginTxn()
        {
            lock (this)
            {
                bool trancationEstablished=false;
                try
                {
                    string tidReply = coordinator.BeginTxn();
                    Console.WriteLine("Coordinator reply : " + tidReply);
                    string[] tempSep = tidReply.Split(Constants.SEP_CHAR_COLON);
                    long receivedTimeStamp = long.Parse(tempSep[1]);
                    if (receivedTimeStamp != info.AvailableMasterMapTimeStamp)
                    {
                        info.AvailableMasterMapTimeStamp = receivedTimeStamp;
                        LoadServerMap();
                    }
                    transactionId=long.Parse(tempSep[0]);
                    trancationEstablished=true;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return trancationEstablished;
            }
        }

        public bool TxCommit(int uid)
        {
            bool isCommited = false;
            //TODO:Call coordinator.

            PadInt padInt = clientAccessedPadInts[uid];
            padInt.Commit(transactionId);
            master.DumpObjectServerStatus();
            return isCommited;
        }

        private int GetModuloServer(int uid)
        {
            lock (this)
            {
                int index = -1;
                if (info.ObjectServerMap.Length > 0)
                    index = uid % info.ObjectServerMap.Length;
                return index;
            }
        }

        private void LoadServerMap()
        {
            lock (this)
            {
                info.ObjectServerMap = master.WorkerServerList.ToArray();
                workers.Clear();
                PADI_Worker worker;
                foreach (var server in info.ObjectServerMap)
                {
                    string ip = server.ServerIp;
                    string port = server.ServerPort;
                    worker = (PADI_Worker)Activator.GetObject(typeof(PADI_Worker), Common.GenerateTcpUrl(ip, port, Constants.OBJECT_TYPE_PADI_WORKER));
                    workers.Add(worker);
                }
                Console.WriteLine("Loaded the new map, size=" + info.ObjectServerMap.Count());
            }
        }
    }

    [Serializable]
    class Information
    {
        private long availableMasterMapTimeStamp;

        public long AvailableMasterMapTimeStamp
        {
            get { return availableMasterMapTimeStamp; }
            set { availableMasterMapTimeStamp = value; }
        }

        private ObjectServer[] objectServerMap;

        public ObjectServer[] ObjectServerMap
        {
            get { return objectServerMap; }
            set { objectServerMap = value; }
        }


    }
}
