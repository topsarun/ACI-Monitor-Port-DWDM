using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;

using RestSharp;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using SnmpSharpNet;

using System.Runtime.Remoting.Channels.Tcp;

namespace ACI_Monitor_01
{
    public partial class Main : Form
    {
        #region<CONFIG_VAR>
        //อ่านค่าจาก File config
        string apicIP = ConfigurationManager.AppSettings.Get("APIC-1_IP");
        string apic_id = ConfigurationManager.AppSettings.Get("APIC_ID");
        string apic_pw = ConfigurationManager.AppSettings.Get("APIC_PASS");
        string line_token = ConfigurationManager.AppSettings.Get("Line_token");
        int MAX_RETRY = int.Parse(ConfigurationManager.AppSettings.Get("MAX_RETRY"));
        int MAX_Shutdown = int.Parse(ConfigurationManager.AppSettings.Get("MAX_Shutdown"));

        int log_101_count = 0, log_id_101_number = 1;
        int log_102_count = 0, log_id_102_number = 1;
        int log_301_count = 0, log_id_301_number = 1;
        int log_302_count = 0, log_id_302_number = 1;
        int log_611_count = 0, log_id_611_number = 1;
        int log_621_count = 0, log_id_621_number = 1;
        int log_612_count = 0, log_id_612_number = 1;
        int log_622_count = 0, log_id_622_number = 1;

        string operState_101_eth17, adminState_101_eth17;
        string operState_102_eth17, adminState_102_eth17;
        string operState_301_eth17, adminState_301_eth17;
        string operState_302_eth17, adminState_302_eth17;
        string operState_611_eth54, adminState_611_eth54;
        string operState_621_eth54, adminState_621_eth54;
        string operState_612_eth54, adminState_612_eth54;
        string operState_622_eth54, adminState_622_eth54;

        int log_operState_101_eth17 = 0;
        int log_operState_102_eth17 = 0;
        int log_operState_301_eth17 = 0;
        int log_operState_302_eth17 = 0;
        int log_operState_611_eth54 = 0;
        int log_operState_621_eth54 = 0;
        int log_operState_612_eth54 = 0;
        int log_operState_622_eth54 = 0;

        int check_101_state_count = 0, check_101_log_count = 0;
        int check_102_state_count = 0, check_102_log_count = 0;
        int check_301_state_count = 0, check_301_log_count = 0;
        int check_302_state_count = 0, check_302_log_count = 0;
        int check_611_state_count = 0, check_611_log_count = 0;
        int check_621_state_count = 0, check_621_log_count = 0;
        int check_612_state_count = 0, check_612_log_count = 0;
        int check_622_state_count = 0, check_622_log_count = 0;

        int Login_Retry = 0;
        int auto_shutdown_count = 0;
        int auto_shutdown_101_state = 0;
        int auto_shutdown_102_state = 0;
        int auto_shutdown_301_state = 0;
        int auto_shutdown_302_state = 0;

        RestClient client = new RestClient();
        #endregion

        public Main()
        {
            InitializeComponent(); // LOAD GUI
            client.Timeout = 5000; // TIME OUT JSON 5S
            TcpChannel channel = new TcpChannel(10101);
        }

        #region<Login_API>
        //LOGIN API
        private Boolean Login_API(string username, string password)
        {
            string sessionId = "", payload = "";
            RestRequest login_post;
            IRestResponse login_post_response;
            JObject login_data;

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/aaaLogin.json");
            client.CookieContainer = new System.Net.CookieContainer();
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;

            payload = "payload{\"aaaUser\":{\"attributes\":{\"name\":\"" + username + "\", \"pwd\":\"" + password + "\"}}}";

            login_post = new RestRequest(Method.POST);
            login_post.AddHeader("content-type", "application/json");
            login_post.AddParameter("application/json", payload, ParameterType.RequestBody);

            try
            {
                login_post_response = client.Execute(login_post);
                login_data = JObject.Parse(login_post_response.Content);
                //LineNotify("ID " + username + " Login");
                sessionId = (login_data["imdata"][0]["aaaLogin"]["attributes"]["sessionId"].ToString());

                Status_Connect.Text = "sessionId = " + sessionId;

                //เปิดการทำงานของ TIMER
                Interface_101_17_timer.Enabled = true;
                Interface_102_17_timer.Enabled = true;
                Interface_301_17_timer.Enabled = true;
                Interface_302_17_timer.Enabled = true;

                Interface_611_54_timer.Enabled = true;
                Interface_621_54_timer.Enabled = true;
                Interface_612_54_timer.Enabled = true;
                Interface_622_54_timer.Enabled = true;

                Log_101_17_timer.Enabled = true;
                Log_102_17_timer.Enabled = true;
                Log_301_17_timer.Enabled = true;
                Log_302_17_timer.Enabled = true;

                Log_611_54_timer.Enabled = true;
                Log_621_54_timer.Enabled = true;
                Log_612_54_timer.Enabled = true;
                Log_622_54_timer.Enabled = true;

                AAA_Refresh.Enabled = true;
                //Keepalive.Enabled = true;

            }
            catch
            {
                Status_Connect.Text = "Can't login to " + apicIP;
                LineNotify("Can't login to " + apicIP);
                MessageBox.Show("Can't login to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true; // no error
        }
#endregion
        
        #region<FirstLog_GET>
        //อ่าน log R1 ครั้งแรก
        private bool Get_first_log_R1()
        {
            log_101_count = 0;
            log_611_count = 0;

            JObject datastat;
            string descr_filed101, code_filed101;

            //POD-1 NODE-101 PORT-ETH1/17
            RestRequest request_101 = new RestRequest();
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-101/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_101 = new RestRequest(Method.GET);
            request_101.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response101 = client.Execute(request_101);
                datastat = JObject.Parse(response101.Content);
                log_101_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_101.Text = log_101_count + "\r\n";
                for (int i = log_101_count - 1; i >= 0; i--)
                {
                    code_filed101 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed101 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_101.Text += "ID " + log_id_101_number++ + " Code " + code_filed101 + " " + descr_filed101 + "\r\n";
                }
                log_101_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================

            //POD-1 NODE-611 PORT-ETH1/54
            RestRequest request_611 = new RestRequest(Method.GET);
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-611/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_611 = new RestRequest();
            request_611.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response611 = client.Execute(request_611);
                datastat = JObject.Parse(response611.Content);
                log_611_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_611.Text = log_611_count + "\r\n";
                string descr_filed611, code_filed611;
                for (int i = log_611_count - 1; i >= 0; i--)
                {
                    code_filed611 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed611 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_611.Text += "ID " + log_id_611_number++ + " Code " + code_filed611 + " " + descr_filed611 + "\r\n";
                }
                log_611_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================
            return true; // no error
        }

        //อ่าน log R2 ครั้งแรก
        private bool Get_first_log_R2()
        {
            log_102_count = 0;
            log_621_count = 0;

            JObject datastat;
            string descr_filed102, code_filed102;

            //POD-1 NODE-102 PORT-ETH1/17
            RestRequest request_102 = new RestRequest();
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-102/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_102 = new RestRequest();
            request_102.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response102 = client.Execute(request_102);
                datastat = JObject.Parse(response102.Content);
                log_102_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_102.Text = log_102_count + "\r\n";
                for (int i = log_102_count - 1; i >= 0; i--)
                {
                    code_filed102 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed102 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_102.Text += "ID " + log_id_102_number++ + " Code " + code_filed102 + " " + descr_filed102 + "\r\n";
                }
                log_102_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================

            //POD-1 NODE-621 PORT-ETH1/54
            RestRequest request_621 = new RestRequest(Method.GET);
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-621/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_621 = new RestRequest(Method.GET);
            request_621.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response621 = client.Execute(request_621);
                datastat = JObject.Parse(response621.Content);
                log_621_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_621.Text = log_621_count + "\r\n";
                string descr_filed621, code_filed621;
                for (int i = log_621_count - 1; i >= 0; i--)
                {
                    code_filed621 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed621 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_621.Text += "ID " + log_id_621_number++ + " Code " + code_filed621 + " " + descr_filed621 + "\r\n";
                }
                log_621_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================
            return true; // no error
        }

        //อ่าน log R3 ครั้งแรก
        private bool Get_first_log_R3()
        {
            log_301_count = 0;
            log_612_count = 0;

            JObject datastat;
            string descr_filed301, code_filed301;

            //POD-1 NODE-301 PORT-ETH1/17
            RestRequest request_301 = new RestRequest();
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-301/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_301 = new RestRequest(Method.GET);
            request_301.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response301 = client.Execute(request_301);
                datastat = JObject.Parse(response301.Content);
                log_301_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_301.Text = log_301_count + "\r\n";
                for (int i = log_301_count - 1; i >= 0; i--)
                {
                    code_filed301 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed301 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_301.Text += "ID " + log_id_301_number++ + " Code " + code_filed301 + " " + descr_filed301 + "\r\n";
                }
                log_301_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================

            //POD-1 NODE-612 PORT-ETH1/54
            RestRequest request_612 = new RestRequest();
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-612/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_612 = new RestRequest(Method.GET);
            request_612.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response612 = client.Execute(request_612);
                datastat = JObject.Parse(response612.Content);
                log_612_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_612.Text = log_612_count + "\r\n";
                string descr_filed612, code_filed612;
                for (int i = log_612_count - 1; i >= 0; i--)
                {
                    code_filed612 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed612 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_612.Text += "ID " + log_id_612_number++ + " Code " + code_filed612 + " " + descr_filed612 + "\r\n";
                }
                log_612_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================
            return true; // no error
        }

        //อ่าน log R4 ครั้งแรก
        private bool Get_first_log_R4()
        {
            log_302_count = 0;
            log_622_count = 0;

            JObject datastat;
            string descr_filed302, code_filed302;

            //POD-1 NODE-302 PORT-ETH1/17
            RestRequest request_302 = new RestRequest();
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-302/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_302 = new RestRequest(Method.GET);
            request_302.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response302 = client.Execute(request_302);
                datastat = JObject.Parse(response302.Content);
                log_302_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_302.Text = log_302_count + "\r\n";
                for (int i = log_302_count - 1; i >= 0; i--)
                {
                    code_filed302 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed302 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_302.Text += "ID " + log_id_302_number++ + " Code " + code_filed302 + " " + descr_filed302 + "\r\n";
                }
                log_302_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================

            //POD-1 NODE-622 PORT-ETH1/54
            RestRequest request_622 = new RestRequest();
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-622/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc");
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;

            request_622 = new RestRequest(Method.GET);
            request_622.AddHeader("cache-control", "no-cache");

            try
            {
                IRestResponse response622 = client.Execute(request_622);
                datastat = JObject.Parse(response622.Content);
                log_622_count = int.Parse((datastat["totalCount"].ToString()));
                LogBox_622.Text = log_622_count + "\r\n";
                string descr_filed622, code_filed622;
                for (int i = log_622_count - 1; i >= 0; i--)
                {
                    code_filed622 = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed622 = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());

                    LogBox_622.Text += "ID " + log_id_622_number++ + " Code " + code_filed622 + " " + descr_filed622 + "\r\n";
                }
                log_622_count++;
            }
            catch
            {
                Status_Connect.Text = "Can't connect to " + apicIP;
                LineNotify("Can't connect to " + apicIP);
                MessageBox.Show("Can't connect to " + apicIP, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //===================================================================================================================
            return true; // no error
        }
#endregion

        #region<INTERFACE_UPDATE>
        private void Interface_101_17_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "101";
            string port = "eth1/17";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_101_eth17 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_101_eth17_led.BackColor = Color.Yellow;
                check_101_state_count++;
                if (check_101_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 101 Eth17");
                    MessageBox.Show("Can't Get status 101 Eth17");
                    Interface_101_17_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_101_eth17 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());

            }
            catch
            {
                operState_101_eth17_led.BackColor = Color.Yellow;
                check_101_state_count++;
                if (check_101_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 101 Eth17");
                    MessageBox.Show("Can't Get status 101 Eth17");
                    Interface_101_17_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 101 ETH1/1 ===============
            if (adminState_101_eth17 == "up")
            {
                adminState_101_eth17_led.BackColor = Color.Green;
            }
            else
            {
                adminState_101_eth17_led.BackColor = Color.Red;
            }
            if (operState_101_eth17 == "up")
            {
                operState_101_eth17_led.BackColor = Color.Green;
            }
            else
            {
                operState_101_eth17_led.BackColor = Color.Red;
            }
            // ====================================================

            check_101_state_count = 0; //NO ERROR
        }

        private void Interface_102_17_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "102";
            string port = "eth1/17";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_102_eth17 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_102_eth17_led.BackColor = Color.Yellow;
                check_102_state_count++;
                if (check_102_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 102 Eth17");
                    MessageBox.Show("Can't Get status 102 Eth17");
                    Interface_102_17_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_102_eth17 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());
            }
            catch
            {
                operState_102_eth17_led.BackColor = Color.Yellow;
                check_102_state_count++;
                if (check_102_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 102 Eth17");
                    MessageBox.Show("Can't Get status 102 Eth17");
                    Interface_102_17_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 102 ETH1/1 ===============
            if (adminState_102_eth17 == "up")
            {
                adminState_102_eth17_led.BackColor = Color.Green;
            }
            else
            {
                adminState_102_eth17_led.BackColor = Color.Red;
            }
            if (operState_102_eth17 == "up")
            {
                operState_102_eth17_led.BackColor = Color.Green;
            }
            else
            {
                operState_102_eth17_led.BackColor = Color.Red;
            }
            // ====================================================

            check_102_state_count = 0; //NO ERROR
        }

        private void Interface_301_17_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "301";
            string port = "eth1/17";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_301_eth17 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_301_eth17_led.BackColor = Color.Yellow;
                check_301_state_count++;
                if (check_301_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 301 Eth17");
                    MessageBox.Show("Can't Get status 301 Eth17");
                    Interface_301_17_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_301_eth17 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());
            }
            catch
            {
                operState_301_eth17_led.BackColor = Color.Yellow;
                check_301_state_count++;
                if (check_301_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 301 Eth17");
                    MessageBox.Show("Can't Get status 301 Eth17");
                    Interface_301_17_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 301 ETH1/1 ===============
            if (adminState_301_eth17 == "up")
            {
                adminState_301_eth17_led.BackColor = Color.Green;
            }
            else
            {
                adminState_301_eth17_led.BackColor = Color.Red;
            }
            if (operState_301_eth17 == "up")
            {
                operState_301_eth17_led.BackColor = Color.Green;
            }
            else
            {
                operState_301_eth17_led.BackColor = Color.Red;
            }
            // ====================================================

            check_301_state_count = 0; //NO ERROR
        }

        private void Interface_302_17_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "302";
            string port = "eth1/17";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_302_eth17 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_302_eth17_led.BackColor = Color.Yellow;
                check_302_state_count++;
                if (check_302_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 302 Eth17");
                    MessageBox.Show("Can't Get status 302 Eth17");
                    Interface_302_17_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_302_eth17 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());
            }
            catch
            {
                operState_302_eth17_led.BackColor = Color.Yellow;
                check_302_state_count++;
                if (check_302_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 302 Eth17");
                    MessageBox.Show("Can't Get status 302 Eth17");
                    Interface_302_17_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 302 ETH1/1 ===============
            if (adminState_302_eth17 == "up")
            {
                adminState_302_eth17_led.BackColor = Color.Green;
            }
            else
            {
                adminState_302_eth17_led.BackColor = Color.Red;
            }
            if (operState_302_eth17 == "up")
            {
                operState_302_eth17_led.BackColor = Color.Green;
            }
            else
            {
                operState_302_eth17_led.BackColor = Color.Red;
            }
            // ====================================================

            check_302_state_count = 0; //NO ERROR
        }

        private void Interface_611_54_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "611";
            string port = "eth1/54";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_611_eth54 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_611_eth54_led.BackColor = Color.Yellow;
                check_611_state_count++;
                if (check_611_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 611 Eth54");
                    MessageBox.Show("Can't Get status 611 Eth54");
                    Interface_611_54_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_611_eth54 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());
            }
            catch
            {
                operState_611_eth54_led.BackColor = Color.Yellow;
                check_611_state_count++;
                if (check_611_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 611 Eth54");
                    MessageBox.Show("Can't Get status 611 Eth54");
                    Interface_611_54_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 611 ETH1/1 ===============
            if (adminState_611_eth54 == "up")
            {
                adminState_611_eth54_led.BackColor = Color.Green;
            }
            else
            {
                adminState_611_eth54_led.BackColor = Color.Red;
            }
            if (operState_611_eth54 == "up")
            {
                operState_611_eth54_led.BackColor = Color.Green;
            }
            else
            {
                operState_611_eth54_led.BackColor = Color.Red;
            }
            // ====================================================

            check_611_state_count = 0; //NO ERROR
        }

        private void Interface_621_54_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "621";
            string port = "eth1/54";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_621_eth54 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_621_eth54_led.BackColor = Color.Yellow;
                check_621_state_count++;
                if (check_621_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 621 Eth54");
                    MessageBox.Show("Can't Get status 621 Eth54");
                    Interface_621_54_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_621_eth54 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());
            }
            catch
            {
                operState_621_eth54_led.BackColor = Color.Yellow;
                check_621_state_count++;
                if (check_621_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 621 Eth54");
                    MessageBox.Show("Can't Get status 621 Eth54");
                    Interface_621_54_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 621 ETH1/1 ===============
            if (adminState_621_eth54 == "up")
            {
                adminState_621_eth54_led.BackColor = Color.Green;
            }
            else
            {
                adminState_621_eth54_led.BackColor = Color.Red;
            }
            if (operState_621_eth54 == "up")
            {
                operState_621_eth54_led.BackColor = Color.Green;
            }
            else
            {
                operState_621_eth54_led.BackColor = Color.Red;
            }
            // ====================================================

            check_621_state_count = 0; //NO ERROR
        }

        private void Interface_612_54_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "612";
            string port = "eth1/54";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_612_eth54 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_612_eth54_led.BackColor = Color.Yellow;
                check_612_state_count++;
                if (check_612_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 612 Eth54");
                    MessageBox.Show("Can't Get status 612 Eth54");
                    Interface_612_54_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_612_eth54 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());
            }
            catch
            {
                operState_612_eth54_led.BackColor = Color.Yellow;
                check_612_state_count++;
                if (check_612_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 612 Eth54");
                    MessageBox.Show("Can't Get status 612 Eth54");
                    Interface_612_54_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 612 ETH1/1 ===============
            if (adminState_612_eth54 == "up")
            {
                adminState_612_eth54_led.BackColor = Color.Green;
            }
            else
            {
                adminState_612_eth54_led.BackColor = Color.Red;
            }
            if (operState_612_eth54 == "up")
            {
                operState_612_eth54_led.BackColor = Color.Green;
            }
            else
            {
                operState_612_eth54_led.BackColor = Color.Red;
            }
            // ====================================================

            check_612_state_count = 0; //NO ERROR
        }

        private void Interface_622_54_timer_Tick(object sender, EventArgs e)
        {
            RestRequest request1;
            IRestResponse response1;
            JObject datastat;
            string urlmoniter;

            string pod = "pod-1";
            string node = "622";
            string port = "eth1/54";
            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                adminState_622_eth54 = (datastat["imdata"][0]["l1PhysIf"]["attributes"]["adminSt"].ToString());
            }
            catch
            {
                adminState_622_eth54_led.BackColor = Color.Yellow;
                check_622_state_count++;
                if (check_622_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 622 Eth54");
                    MessageBox.Show("Can't Get status 622 Eth54");
                    Interface_622_54_timer.Enabled = false;
                }
                return;
            }

            urlmoniter = "https://" + apicIP + "/api/node/mo/topology/" + pod + "/node-" + node + "/sys/phys-[" + port + "].json?query-target=children&target-subtree-class=ethpmPhysIf&rsp-subtree=no";
            client.BaseUrl = new System.Uri(urlmoniter);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request1 = new RestRequest(Method.GET);
            request1.AddHeader("cache-control", "no-cache");
            try
            {
                response1 = client.Execute(request1);
                datastat = JObject.Parse(response1.Content);
                operState_622_eth54 = (datastat["imdata"][0]["ethpmPhysIf"]["attributes"]["operSt"].ToString());
            }
            catch
            {
                operState_622_eth54_led.BackColor = Color.Yellow;
                check_622_state_count++;
                if (check_622_state_count >= MAX_RETRY)
                {
                    LineNotify("Can't Get status 622 Eth54");
                    MessageBox.Show("Can't Get status 622 Eth54");
                    Interface_622_54_timer.Enabled = false;
                }
                return;
            }

            // =============== POD1 NODE 622 ETH1/1 ===============
            if (adminState_622_eth54 == "up")
            {
                adminState_622_eth54_led.BackColor = Color.Green;
            }
            else
            {
                adminState_622_eth54_led.BackColor = Color.Red;
            }
            if (operState_622_eth54 == "up")
            {
                operState_622_eth54_led.BackColor = Color.Green;
            }
            else
            {
                operState_622_eth54_led.BackColor = Color.Red;
            }
            // ====================================================

            check_622_state_count = 0; //NO ERROR
        }
#endregion

        #region<LOG_UPDATE>
        private void Log_101_17_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-101/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_101_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_101_eth17 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_101_eth17 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                        if (auto_shutdown_101_state == 1)
                        {
                            auto_shutdown_101_state = 0;
                            auto_shutdown_count--;
                        }
                    }
                    LogBox_101.Text += "ID " + log_id_101_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_101_count = log_check + 1;
                check_101_log_count = 0;
            }
            catch
            {
                check_101_log_count++;
                Status_Connect.Text = "Can't get log NODE-101 Eth1/17 #" + check_101_log_count;
                if (check_101_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-101 Eth1/17");
                    MessageBox.Show("Can't get log NODE-101 Eth1/17");
                    Log_101_17_timer.Enabled = false;
                }
            }

        }

        private void Log_102_17_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-102/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_102_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_102_eth17 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_102_eth17 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                        if (auto_shutdown_102_state == 1)
                        {
                            auto_shutdown_102_state = 0;
                            auto_shutdown_count--;
                        }
                    }
                    LogBox_102.Text += "ID " + log_id_102_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_102_count = log_check + 1;
                check_102_log_count = 0;
            }
            catch
            {
                check_102_log_count++;
                Status_Connect.Text = "Can't get log NODE-102 Eth1/17 #" + check_102_log_count;
                if (check_102_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-102 Eth1/17");
                    MessageBox.Show("Can't get log NODE-102 Eth1/17");
                    Log_102_17_timer.Enabled = false;
                }
            }

        }

        private void Log_301_17_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-301/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_301_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_301_eth17 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_301_eth17 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                        if (auto_shutdown_301_state == 1)
                        {
                            auto_shutdown_301_state = 0;
                            auto_shutdown_count--;
                        }
                    }
                    LogBox_301.Text += "ID " + log_id_301_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_301_count = log_check + 1;
                check_301_log_count = 0;
            }
            catch
            {
                check_301_log_count++;
                Status_Connect.Text = "Can't get log NODE-301 Eth1/17 #" + check_301_log_count;
                if (check_301_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-301 Eth1/17");
                    MessageBox.Show("Can't get log NODE-301 Eth1/17");
                    Log_301_17_timer.Enabled = false;
                }
            }
        }

        private void Log_302_17_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-302/sys/phys-[eth1/17].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_302_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_302_eth17 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_302_eth17 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                        if (auto_shutdown_302_state == 1)
                        {
                            auto_shutdown_302_state = 0;
                            auto_shutdown_count--;
                        }
                    }
                    LogBox_302.Text += "ID " + log_id_302_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_302_count = log_check + 1;
                check_302_log_count = 0;
            }
            catch
            {
                check_302_log_count++;
                Status_Connect.Text = "Can't get log NODE-302 Eth1/17 #" + check_302_log_count;
                if (check_302_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-302 Eth1/17");
                    MessageBox.Show("Can't get log NODE-302 Eth1/17");
                    Log_302_17_timer.Enabled = false;
                }
            }

        }

        private void Log_611_54_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-611/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_611_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_611_eth54 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_611_eth54 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    LogBox_611.Text += "ID " + log_id_611_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_611_count = log_check + 1;
                check_611_log_count = 0;
            }
            catch
            {
                check_611_log_count++;
                Status_Connect.Text = "Can't get log NODE-611 Eth1/54 #" + check_611_log_count;
                if (check_611_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-611 Eth1/54");
                    MessageBox.Show("Can't get log NODE-611 Eth1/54");
                    Log_611_54_timer.Enabled = false;
                }
            }

        }

        private void Log_621_54_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-621/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_621_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_621_eth54 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_612_eth54 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    LogBox_621.Text += "ID " + log_id_621_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_621_count = log_check + 1;
                check_621_log_count = 0;
            }
            catch
            {
                check_621_log_count++;
                Status_Connect.Text = "Can't get log NODE-621 Eth1/54 #" + check_621_log_count;
                if (check_621_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-621 Eth1/54");
                    MessageBox.Show("Can't get log NODE-621 Eth1/54");
                    Log_621_54_timer.Enabled = false;
                }
            }

        }

        private void Log_612_54_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-612/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_612_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_612_eth54 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_612_eth54 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    LogBox_612.Text += "ID " + log_id_612_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_612_count = log_check + 1;
                check_612_log_count = 0;
            }
            catch
            {
                check_612_log_count++;
                Status_Connect.Text = "Can't get log NODE-612 Eth1/54 #" + check_612_log_count;
                if (check_612_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-612 Eth1/54");
                    MessageBox.Show("Can't get log NODE-612 Eth1/54");
                    Log_612_54_timer.Enabled = false;
                }
            }

        }

        private void Log_622_54_timer_Tick(object sender, EventArgs e)
        {
            var request = new RestRequest(Method.GET);

            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/topology/pod-1/node-622/sys/phys-[eth1/54].json?rsp-subtree-include=event-logs,no-scoped,subtree&order-by=eventRecord.created|desc&page=0&page-size=25");
            request = new RestRequest(Method.GET);
            ServicePointManager.ServerCertificateValidationCallback += (RestRequest, certificate, chain, sslPolicyErrors) => true;
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response1 = client.Execute(request);

            try
            {
                JObject datastat = JObject.Parse(response1.Content);
                int log_check = int.Parse((datastat["totalCount"].ToString()));

                string descr_filed, code_filed, changeSet_filed, affected;

                for (int i = log_check - log_622_count; i >= 0; i--)
                {
                    code_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["code"].ToString());
                    descr_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["descr"].ToString());
                    changeSet_filed = (datastat["imdata"][i]["eventRecord"]["attributes"]["changeSet"].ToString());
                    bool check_word = changeSet_filed.Contains("link-failure");

                    if (code_filed == "E4205126" && check_word)
                    {
                        log_operState_622_eth54 = 1;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    else if (code_filed == "E4205125")
                    {
                        log_operState_622_eth54 = 0;
                        affected = (datastat["imdata"][i]["eventRecord"]["attributes"]["affected"].ToString());
                        LineNotify(affected + " " + descr_filed);
                    }
                    LogBox_622.Text += "ID " + log_id_622_number++ + " Code " + code_filed + " " + descr_filed + "\r\n";
                }
                log_622_count = log_check + 1;
                check_622_log_count = 0;
            }
            catch
            {
                check_622_log_count++;
                Status_Connect.Text = "Can't get log NODE-622 Eth1/54 #" + check_622_log_count;
                if (check_622_log_count >= MAX_RETRY)
                {
                    LineNotify("Can't get log NODE-622 Eth1/54");
                    MessageBox.Show("Can't get log NODE-622 Eth1/54");
                    Log_622_54_timer.Enabled = false;
                }
            }

        }
        #endregion

        #region<MENU_ITEM>
        private void LoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // LOGIN MENU
            Login_Retry = 0;
            while (Login_API(apic_id, apic_pw) == false && Login_Retry <= MAX_RETRY)
            {
                Login_Retry++;
                Status_Connect.Text = "Can't connect to " + apicIP + " #" + Login_Retry;
            }
            if (Login_Retry > MAX_RETRY)
            {
                LineNotify("Can't connect to " + apicIP);
            }
            else
            {
                Login_Retry = 0;

                LogBox_102.Text = "";
                LogBox_611.Text = "";
                LogBox_101.Text = "";
                LogBox_621.Text = "";
                LogBox_301.Text = "";
                LogBox_612.Text = "";
                LogBox_301.Text = "";
                LogBox_612.Text = "";

                Get_first_log_R1();
                Get_first_log_R2();
                Get_first_log_R3();
                Get_first_log_R4();
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // EXIT MENU
            Application.Exit();
        }

        private void FormShutdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form Shutdown = new Shutdown();
            Shutdown.ShowDialog();
        }
        #endregion

        #region<TEXTBOX_TEXT_CHANGE>
        private void LogBox_101_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_101.SelectionStart = LogBox_101.Text.Length;
            LogBox_101.ScrollToCaret();
        }

        private void LogBox_611_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_611.SelectionStart = LogBox_611.Text.Length;
            LogBox_611.ScrollToCaret();
        }

        private void LogBox_102_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_102.SelectionStart = LogBox_102.Text.Length;
            LogBox_102.ScrollToCaret();
        }

        private void LogBox_621_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_621.SelectionStart = LogBox_621.Text.Length;
            LogBox_621.ScrollToCaret();
        }

        private void LogBox_301_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_301.SelectionStart = LogBox_301.Text.Length;
            LogBox_301.ScrollToCaret();
        }

        private void LogBox_612_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_612.SelectionStart = LogBox_612.Text.Length;
            LogBox_612.ScrollToCaret();
        }

        private void LogBox_302_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_302.SelectionStart = LogBox_302.Text.Length;
            LogBox_302.ScrollToCaret();
        }

        private void LogBox_622_TextChanged(object sender, EventArgs e)
        {
            // AUTO UPDATE GUI
            LogBox_622.SelectionStart = LogBox_622.Text.Length;
            LogBox_622.ScrollToCaret();
        }
        #endregion

        #region<ROUTE_CHECK_FUNCTION>
        private void Route_check_timer_Tick(object sender, EventArgs e)
        {
            if (operState_101_eth17 == "down" && operState_611_eth54 == "down" && log_operState_101_eth17 == 1 && log_operState_611_eth54 == 1)
            {
                LineNotify("DWDM TO SRB ROUTE 1 DOWN NOW !");
                log_operState_101_eth17 = 0;
                log_operState_611_eth54 = 0;

                if (auto_shutdown_count < MAX_Shutdown)
                {
                    auto_shutdown_count++;
                    auto_shutdown_101_state = 1;
                    Shutdown_R1();
                }
                else
                {
                    LineNotify("MAXIMUM AUTO SHUTDOWN");
                }

            }
            if (operState_102_eth17 == "down" && operState_621_eth54 == "down" && log_operState_102_eth17 == 1 && log_operState_621_eth54 == 1)
            {
                LineNotify("DWDM TO SRB ROUTE 2 DOWN NOW !");
                log_operState_102_eth17 = 0;
                log_operState_621_eth54 = 0;

                if (auto_shutdown_count < MAX_Shutdown)
                {
                    auto_shutdown_count++;
                    auto_shutdown_102_state = 1;
                    Shutdown_R2();
                }
                else
                {
                    LineNotify("MAXIMUM AUTO SHUTDOWN");
                }
            }
            if (operState_301_eth17 == "down" && operState_612_eth54 == "down" && log_operState_301_eth17 == 1 && log_operState_612_eth54 == 1)
            {
                LineNotify("DWDM TO SRB ROUTE 3 DOWN NOW !");
                log_operState_301_eth17 = 0;
                log_operState_612_eth54 = 0;

                if (auto_shutdown_count < MAX_Shutdown)
                {
                    auto_shutdown_count++;
                    auto_shutdown_301_state = 1;
                    Shutdown_R3();
                }
                else
                {
                    LineNotify("MAXIMUM AUTO SHUTDOWN");
                }
            }
            if (operState_302_eth17 == "down" && operState_622_eth54 == "down" && log_operState_302_eth17 == 1 && log_operState_622_eth54 == 1)
            {
                LineNotify("DWDM TO SRB ROUTE 4 DOWN NOW !");
                log_operState_302_eth17 = 0;
                log_operState_622_eth54 = 0;

                if (auto_shutdown_count < MAX_Shutdown)
                {
                    auto_shutdown_count++;
                    auto_shutdown_302_state = 1;
                    Shutdown_R4();
                }
                else
                {
                    LineNotify("MAXIMUM AUTO SHUTDOWN");
                }
            }
        }
        #endregion

        #region<SHUTDOWN_FUNCTION>
        private void Shutdown_R1()
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            // NODE-101 Eth1/17
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-101/pathep-[eth1/17]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-101 eth1/17 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }

            // NODE-611 Eth1/54
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-611/pathep-[eth1/54]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-611 eth1/54 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }
        }

        private void Shutdown_R2()
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            // NODE-102 Eth1/17
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-102/pathep-[eth1/17]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-102 eth1/17 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }
            // NODE-621 Eth1/54
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-621/pathep-[eth1/54]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-621 eth1/17 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }
        }

        private void Shutdown_R3()
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            // NODE-301 Eth1/17
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-301/pathep-[eth1/17]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-301 eth1/17 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }
            // NODE-612 Eth1/54
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-612/pathep-[eth1/54]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-612 eth1/17 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }
        }

        private void Shutdown_R4()
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            // NODE-302 Eth1/17
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-302/pathep-[eth1/17]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-302 eth1/17 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }
            // NODE-622 Eth1/54
            client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
            Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"tDn\":\"topology/pod-1/paths-622/pathep-[eth1/54]\",\"lc\":\"blacklist\"},\"children\":[]}}";

            request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", Input, ParameterType.RequestBody);

            try
            {
                response = client.Execute(request);
                LineNotify("Node-622 eth1/17 auto shutdown");
            }
            catch
            {
                LineNotify("Error to Auto shutdown command!");
            }
        }
        #endregion

        #region<FUNCTIO_ETC>
        private void AAA_Refresh_Tick(object sender, EventArgs e)
        {
            // AUTO REFRESH LOGIN 180 S
            while (Login_API(apic_id, apic_pw) == false && Login_Retry <= MAX_RETRY)
            {
                Login_Retry++;
                Status_Connect.Text = "Can't connect to " + apicIP + " #" + Login_Retry;
            }
            if (Login_Retry > MAX_RETRY)
            {
                LineNotify("Can't connect to " + apicIP);
            }
            else
            {
                Login_Retry = 0;
            }
        }

        private void Keepalive_Tick(object sender, EventArgs e)
        {
            //Keepalive
            //LineNotify("Keepalive Check");
        }

        private int LineNotify(string msg)
        {
            RestRequest line_post;
            RestClient line_client = new RestClient();

            line_client.BaseUrl = new System.Uri("https://notify-api.line.me/api/notify");
            ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;

            line_post = new RestRequest(Method.POST); ;
            line_post.AddHeader("Authorization", string.Format("Bearer " + line_token));
            line_post.AddHeader("content-type", "application/x-www-form-urlencoded");
            line_post.AddParameter("message", msg);

            try
            {
                IRestResponse response = line_client.Execute(line_post);
                return int.Parse((JObject.Parse(response.Content)["status"].ToString()));
            }
            catch
            {
                Status_Connect.Text = "Can't connect to line server !!";
                MessageBox.Show("Can't connect to line server !!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
#endregion
    }
}
