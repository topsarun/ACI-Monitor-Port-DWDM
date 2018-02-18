using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Configuration;

using RestSharp;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using SnmpSharpNet;

namespace ACI_Monitor_01
{
    public partial class Shutdown : Form
    {
        string apicIP = ConfigurationManager.AppSettings.Get("APIC-1_IP");
        string line_token = ConfigurationManager.AppSettings.Get("Line_token");

        RestClient client = new RestClient();

        public Shutdown()
        {
            InitializeComponent();
        }

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
                sessionId = (login_data["imdata"][0]["aaaLogin"]["attributes"]["sessionId"].ToString());
                Status_Connect.Text = "sessionId = " + sessionId;

                ShutdownR1_button.Enabled = true;
                ShutdownR2_button.Enabled = true;
                ShutdownR3_button.Enabled = true;
                ShutdownR4_button.Enabled = true;
                EnableR1_button.Enabled = true;
                EnableR2_button.Enabled = true;
                EnableR3_button.Enabled = true;
                EnableR4_button.Enabled = true;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void Login_button_Click(object sender, EventArgs e)
        {
            if (Login_API(ID_BOX.Text, PASS_BOX.Text))
            {
                LineNotify("[ALERT]ID \"" + ID_BOX.Text + "\" login shutdown menu!!");
                MessageBox.Show("Login Complete Be Careful!!!!");
            }
        }

        private void ShutdownR1_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to shutdown POD-1 Node-101 Eth1/17 AND Node-611 Eth1/54 ??", "Confirm Shutdown!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
        }

        private void ShutdownR2_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to shutdown POD-1 Node-102 Eth1/17 AND Node-621 Eth1/54 ??", "Confirm Shutdown!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
        }

        private void ShutdownR3_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to shutdown POD-1 Node-301 Eth1/17 AND Node-612 Eth1/54 ??", "Confirm Shutdown!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
        }

        private void ShutdownR4_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to shutdown POD-1 Node-302 Eth1/17 AND Node-622 Eth1/54 ??", "Confirm Shutdown!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
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
                    MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
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

        private void EnableR1_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to enable R1 ??", "Confirm Enable!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                //POD1 - NODE101 - ETH1/17
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-101/pathep-[eth1/17]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }

                //POD1 - NODE611 - ETH1/54
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-611/pathep-[eth1/54]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
        }

        private void EnableR2_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to enable R2 ??", "Confirm Enable!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                //POD1 - NODE102 - ETH1/17
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-102/pathep-[eth1/17]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }

                //POD1 - NODE621 - ETH1/54
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-621/pathep-[eth1/54]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
        }

        private void EnableR3_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to enable R3 ??", "Confirm Enable!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                //POD1 - NODE301 - ETH1/17
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-301/pathep-[eth1/17]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }

                //POD1 - NODE612 - ETH1/54
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-612/pathep-[eth1/54]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
        }

        private void EnableR4_button_Click(object sender, EventArgs e)
        {
            RestRequest request;
            IRestResponse response;
            string Input;

            var confirmResult = MessageBox.Show("Are you sure to enable R4 ??", "Confirm Enable!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                //POD1 - NODE302 - ETH1/17
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-302/pathep-[eth1/17]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }

                //POD1 - NODE622 - ETH1/54
                client.BaseUrl = new System.Uri("https://" + apicIP + "/api/node/mo/uni/fabric/outofsvc.json");
                ServicePointManager.ServerCertificateValidationCallback += (RestClient, certificate, chain, sslPolicyErrors) => true;
                Input = "payload{\"fabricRsOosPath\":{\"attributes\":{\"dn\":\"uni/fabric/outofsvc/rsoosPath-[topology/pod-1/paths-622/pathep-[eth1/54]]\",\"status\":\"deleted\"},\"children\":[]}}";

                request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", Input, ParameterType.RequestBody);

                try
                {
                    response = client.Execute(request);
                    //MessageBox.Show("run command!");
                }
                catch
                {
                    MessageBox.Show("Error to run command!");
                }
            }
            else
                return;
        }
    }
}
