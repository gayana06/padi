using PADI_LIBRARY;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PADI_CLIENT
{
    public partial class ClientForm : Form
    {

        private const string APP_SET_TASK = "TASK";
        private const string APP_SET_SLEEP_TIME = "SLEEP_TIME";
        private const string BEGIN_TRANSACTION = "BT";
        private const string END_TRANSACTION = "ET";
        private const string CREATE_PADINT = "CPI";
        private const string ACCESS_PADINT = "API";
        private const string READ = "RD";
        private const string WRITE = "WT";
        private const string STATUS_DUMP = "STD";
        private const char SEP_CHAR_COMMA = ',';
        private const string SEP_STR_COMMA = ",";
        private const char SEP_CHAR_COLON = ':';
        private const string SEP_STR_COLON = ":";


        private string[] operationArray;


      //  PADI_Client client;

        public ClientForm()
        {
            InitializeComponent();
           // client = new PADI_Client();
            PADI_Client.Init();
        }

        public void Start()
        {
            try
            {
                //operationArray = ConfigurationManager.AppSettings[APP_SET_TASK].Split(SEP_CHAR_COMMA);
                string commands=tbxOperations.Text.Trim();
                if (!String.IsNullOrEmpty(commands))
                {
                    operationArray = commands.Split(SEP_CHAR_COMMA);
                    string[] tmp;
                    PadInt padInt = null;
                    foreach (var operation in operationArray)
                    {
                        tmp = operation.Split(SEP_CHAR_COLON);
                        bool status = false;
                        switch (tmp[0])
                        {
                            case BEGIN_TRANSACTION:
                                status = PADI_Client.TxBegin();
                                UpdateResultPanel("Transaction started. " + status);
                                break;
                            case END_TRANSACTION:
                                status = PADI_Client.TxCommit();
                                UpdateResultPanel("Transaction committed. " + status);
                                break;
                            case CREATE_PADINT:
                                padInt = PADI_Client.CreatePadInt(Int32.Parse(tmp[1]));
                                break;
                            case ACCESS_PADINT:
                                padInt = PADI_Client.AccessPadInt(Int32.Parse(tmp[1]));
                                break;
                            case READ:
                                if (padInt != null)
                                    UpdateResultPanel("Read value = " + padInt.Read());
                                else
                                    UpdateResultPanel("PadInt is null - READ");
                                break;
                            case WRITE:
                                if (padInt != null)
                                {
                                    padInt.Write(Int32.Parse(tmp[1]));
                                    UpdateResultPanel("Write issued = " + tmp[1]);
                                }
                                else
                                    Console.WriteLine("PadInt is null - WRITE");
                                break;
                            case STATUS_DUMP:
                                PADI_Client.Status();
                                UpdateResultPanel("Dumped Status");
                                break;
                        }

                    }
                }
                
            }
            catch (TxException ex)
            {
                UpdateResultPanel(ex.Message);
            }
            catch (Exception ex)
            {
                UpdateResultPanel(ex.Message);
            }

        }



        private void btnExecute_Click(object sender, EventArgs e)
        {
            Start();
        }

        public void UpdateResultPanel(String result)
        {
                tbxResult.Text += "\r\n"+result;          
        }
    }
}
