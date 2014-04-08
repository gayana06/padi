using System;
using System.Collections;
using System.Collections.Generic;

namespace RemotingSample
{
    public class MyRemoteObject : MarshalByRefObject
    {


        public string MetodoOla()
        {
            return "ola!";
        }


    }


}