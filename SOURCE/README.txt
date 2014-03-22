Current source includes,
1. Bootstrap.
2. Heartbeat messages between M and S
3. Failure detection when S leave.
4. Master's worker server view dissamination.

To test current flow,
1. Run Master first.
2. Run several Object servers changing the port number in App.config of the PADI_ObjectServer project.
3. Examin the Master console.
4. Close a worker server.
5. Notice the failure detection in Master console.

Note: Time durations related to fail detection are define in App.config file of PADI_MasterServer project.