How to test
--------------------
1. Just run the bat file.

The operations are defined in client app.config files with tag name <TASK>
Change as you wish to add new operations. Below is the description of commands.

BEGIN_TRANSACTION = BT
END_TRANSACTION = ET
CREATE_PADINT = CPI
ACCESS_PADINT = API
READ = RD
WRITE = WT
STATUS_DUMP = STD
FREEZE = FZ
FAIL = FL
RECOVER = REC

Example: Create a PadInt with UID=1 and write value =100.
BT,CPI-1,WT-100,ET

Read value of the UID = 2 and UID = 4, then write value =999 to UID = 4,finally dump server status
BT,API-2,RD,API-4,RD,WT-999,ET,STD
