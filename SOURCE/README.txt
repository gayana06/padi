How to test the client
-----------------------------
1. In the PADI_CLIENT project, check the app.config file. There is a recored key = "TASK"
2. In the related value section you can see a sequence of operations. below is a sample.

BT,CPI:2,WT:10,ET,STD

above string will begin transaction, create padint with uid 2, write value 10, and end transaction by commit and dump the status to the object server console.

3. So now make several copies of the compiled exe files and change the above string in app.config and test.
4. when the fail, freeze and abort are done, add the those operations to this client as well.

Current source includes,
1. Bootstrap.
2. Heartbeat messages between M and S
3. Failure detection when S leave.
4. Master's worker server view dissamination.
5. Coordinator provides TID.
6. Client request a TID.

To test current flow,
1. Run Master first.
2. Run several Object servers changing the port number in App.config of the PADI_ObjectServer project.
3. Examin the Master console.
4. Close a worker server.
5. Notice the failure detection in Master console.


Note: Time durations related to fail detection are define in App.config file of PADI_MasterServer project.