2014-04-07 22:34:19,769 [1] INFO  Client started,,
2014-04-07 22:34:29,894 [1] INFO  PadInt successfully created, UID = 1,,
2014-04-07 22:34:29,895 [1] INFO  PadInt successfully created, UID = 2,,
2014-04-07 22:34:29,896 [1] INFO  PadInt successfully created, UID = 3,,
2014-04-07 22:34:29,902 [1] INFO  PadInt successfully retrieved, UID = 1,,
2014-04-07 22:34:29,919 [1] INFO  PadInt successfully retrieved, UID = 2,,
2014-04-07 22:34:29,921 [1] INFO  PadInt successfully retrieved, UID = 3,,
2014-04-07 22:35:47,875 [1] INFO  Client started,,
2014-04-07 22:35:57,974 [1] INFO  CreatePadInt for UID = 1 returned null. Already exists,,
2014-04-07 22:35:57,975 [1] INFO  CreatePadInt for UID = 2 returned null. Already exists,,
2014-04-07 22:35:57,976 [1] INFO  CreatePadInt for UID = 3 returned null. Already exists,,
2014-04-07 22:35:57,983 [1] INFO  PadInt successfully retrieved, UID = 1,,
2014-04-07 22:35:57,993 [1] INFO  PadInt successfully retrieved, UID = 2,,
2014-04-07 22:35:58,006 [1] INFO  PadInt successfully retrieved, UID = 3,,
2014-04-07 23:24:46,857 [1] INFO  Client started,,
2014-04-07 23:24:56,958 [1] INFO  PadInt successfully created, UID = 1,,
2014-04-07 23:24:56,959 [1] INFO  PadInt successfully created, UID = 2,,
2014-04-07 23:24:56,959 [1] INFO  PadInt successfully created, UID = 3,,
2014-04-07 23:24:56,962 [1] INFO  PadInt successfully retrieved, UID = 1,,
2014-04-07 23:24:56,980 [1] ERROR Exception of type 'PADI_LIBRARY.TxException' was thrown.,
Server stack trace: 


Exception rethrown at [0]: 
   at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg)
   at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData& msgData, Int32 type)
   at PADI_LIBRARY.PADI_Worker.Write(Int32 uid, Int64 TID, Int32 value)
   at PADI_LIBRARY.PadInt.Write(Int32 value) in e:\EMDC_IST\SEMESTER_2\PADI_Middleware_for_Distributed_Internet_Applications\PROJECT\SOURCE\PADI_DTMS\PADI_LIBRARY\UTIL_CLASSES\PadInt.cs:line 71,mscorlib
2014-04-07 23:27:17,315 [9] INFO  Client started,,
2014-04-07 23:27:27,399 [9] INFO  PadInt successfully created, UID = 1,,
2014-04-07 23:27:27,400 [9] INFO  PadInt successfully created, UID = 2,,
2014-04-07 23:27:27,401 [9] INFO  PadInt successfully created, UID = 3,,
2014-04-07 23:27:27,405 [9] INFO  PadInt successfully retrieved, UID = 1,,
2014-04-07 23:27:27,509 [9] ERROR Exception of type 'PADI_LIBRARY.TxException' was thrown.,
Server stack trace: 


Exception rethrown at [0]: 
   at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg)
   at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData& msgData, Int32 type)
   at PADI_LIBRARY.PADI_Worker.Write(Int32 uid, Int64 TID, Int32 value)
   at PADI_LIBRARY.PadInt.Write(Int32 value) in e:\EMDC_IST\SEMESTER_2\PADI_Middleware_for_Distributed_Internet_Applications\PROJECT\SOURCE\PADI_DTMS\PADI_LIBRARY\UTIL_CLASSES\PadInt.cs:line 71,mscorlib
2014-04-07 23:30:55,618 [9] INFO  Client started,,
2014-04-07 23:31:05,731 [9] INFO  PadInt successfully created, UID = 1,,
2014-04-07 23:31:05,732 [9] INFO  PadInt successfully created, UID = 2,,
2014-04-07 23:31:05,733 [9] INFO  PadInt successfully created, UID = 3,,
2014-04-07 23:31:05,738 [9] INFO  PadInt successfully retrieved, UID = 1,,
2014-04-07 23:31:38,204 [9] ERROR Exception of type 'PADI_LIBRARY.TxException' was thrown.,
Server stack trace: 


Exception rethrown at [0]: 
   at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg)
   at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData& msgData, Int32 type)
   at PADI_LIBRARY.PADI_Worker.Write(Int32 uid, Int64 TID, Int32 value)
   at PADI_LIBRARY.PadInt.Write(Int32 value) in e:\EMDC_IST\SEMESTER_2\PADI_Middleware_for_Distributed_Internet_Applications\PROJECT\SOURCE\PADI_DTMS\PADI_LIBRARY\UTIL_CLASSES\PadInt.cs:line 71,mscorlib
2014-04-07 23:45:56,141 [1] INFO  Client started,,
2014-04-07 23:45:58,256 [1] INFO  PadInt successfully created, UID = 1,,
2014-04-07 23:45:58,257 [1] INFO  PadInt successfully created, UID = 2,,
2014-04-07 23:45:58,257 [1] INFO  PadInt successfully created, UID = 3,,
2014-04-07 23:45:58,264 [1] INFO  PadInt successfully retrieved, UID = 1,,
2014-04-07 23:45:58,286 [1] ERROR Write aborted TID=635325111582099075,
Server stack trace: 
   at PADI_LIBRARY.PADI_Worker.Write(Int32 uid, Int64 TID, Int32 value) in e:\EMDC_IST\SEMESTER_2\PADI_Middleware_for_Distributed_Internet_Applications\PROJECT\SOURCE\PADI_DTMS\PADI_LIBRARY\MAIN_CLASSES\PADI_Worker.cs:line 124
   at System.Runtime.Remoting.Messaging.StackBuilderSink._PrivateProcessMessage(IntPtr md, Object[] args, Object server, Int32 methodPtr, Boolean fExecuteInContext, Object[]& outArgs)
   at System.Runtime.Remoting.Messaging.StackBuilderSink.SyncProcessMessage(IMessage msg, Int32 methodPtr, Boolean fExecuteInContext)

Exception rethrown at [0]: 
   at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg)
   at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData& msgData, Int32 type)
   at PADI_LIBRARY.PADI_Worker.Write(Int32 uid, Int64 TID, Int32 value)
   at PADI_LIBRARY.PadInt.Write(Int32 value) in e:\EMDC_IST\SEMESTER_2\PADI_Middleware_for_Distributed_Internet_Applications\PROJECT\SOURCE\PADI_DTMS\PADI_LIBRARY\UTIL_CLASSES\PadInt.cs:line 71,PADI_LIBRARY
