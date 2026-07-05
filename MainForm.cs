using System;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace UniT161E
{
    public partial class MainForm : Form
    {
        private SerialPort serialPort;
        private Thread dataReadThread;
        private volatile bool isRunning = false;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "UNI-T 161E Multimeter Display";
            this.Size = new System.Drawing.Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            // Main panel
            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(mainPanel);

            // Display label
            Label displayLabel = new Label();
            displayLabel.Name = "DisplayLabel";
            displayLabel.Text = "0.000";
            displayLabel.Font = new System.Drawing.Font("Arial", 72, System.Drawing.FontStyle.Bold);
            displayLabel.ForeColor = System.Drawing.Color.Lime;
            displayLabel.TextAlign = System.Windows.Forms.ContentAlignment.MiddleCenter;
            displayLabel.Dock = DockStyle.Top;
            displayLabel.Height = 120;
            mainPanel.Controls.Add(displayLabel);

            // Status label
            Label statusLabel = new Label();
            statusLabel.Name = "StatusLabel";
            statusLabel.Text = "Disconnected";
            statusLabel.Font = new System.Drawing.Font("Arial", 14);
            statusLabel.ForeColor = System.Drawing.Color.White;
            statusLabel.TextAlign = System.Windows.Forms.ContentAlignment.MiddleCenter;
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Height = 40;
            mainPanel.Controls.Add(statusLabel);

            // Control panel
            Panel controlPanel = new Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.Padding = new Padding(10);
            controlPanel.BackColor = System.Drawing.Color.Black;
            mainPanel.Controls.Add(controlPanel);

            // Port selection
            Label portLabel = new Label();
            portLabel.Text = "Serial Port:";
            portLabel.ForeColor = System.Drawing.Color.White;
            portLabel.Location = new System.Drawing.Point(10, 10);
            portLabel.Size = new System.Drawing.Size(100, 25);
            controlPanel.Controls.Add(portLabel);

            ComboBox portCombo = new ComboBox();
            portCombo.Name = "PortCombo";
            portCombo.Location = new System.Drawing.Point(110, 10);
            portCombo.Size = new System.Drawing.Size(150, 25);
            portCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            string[] ports = SerialPort.GetPortNames();
            portCombo.Items.AddRange(ports);
            if (ports.Length > 0) portCombo.SelectedIndex = 0;
            controlPanel.Controls.Add(portCombo);

            // Baud rate selection
            Label baudLabel = new Label();
            baudLabel.Text = "Baud Rate:";
            baudLabel.ForeColor = System.Drawing.Color.White;
            baudLabel.Location = new System.Drawing.Point(280, 10);
            baudLabel.Size = new System.Drawing.Size(100, 25);
            controlPanel.Controls.Add(baudLabel);

            ComboBox baudCombo = new ComboBox();
            baudCombo.Name = "BaudCombo";
            baudCombo.Location = new System.Drawing.Point(380, 10);
            baudCombo.Size = new System.Drawing.Size(100, 25);
            baudCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            baudCombo.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            baudCombo.SelectedItem = 9600;
            controlPanel.Controls.Add(baudCombo);

            // Connect button
            Button connectButton = new Button();
            connectButton.Name = "ConnectButton";
            connectButton.Text = "Connect";
            connectButton.Location = new System.Drawing.Point(490, 10);
            connectButton.Size = new System.Drawing.Size(90, 25);
            connectButton.Click += ConnectButton_Click;
            controlPanel.Controls.Add(connectButton);

            // Info text
            TextBox infoText = new TextBox();
            infoText.Name = "InfoText";
            infoText.Location = new System.Drawing.Point(10, 50);
            infoText.Size = new System.Drawing.Size(570, 300);
            infoText.Multiline = true;
            infoText.ScrollBars = ScrollBars.Vertical;
            infoText.ReadOnly = true;
            infoText.BackColor = System.Drawing.Color.Black;
            infoText.ForeColor = System.Drawing.Color.Lime;
            infoText.Font = new System.Drawing.Font("Courier New", 10);
            controlPanel.Controls.Add(infoText);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            if (isRunning)
            {
                DisconnectDevice();
                btn.Text = "Connect";
            }
            else
            {
                ConnectDevice();
                btn.Text = "Disconnect";
            }
        }

        private void ConnectDevice()
        {
            try
            {
                ComboBox portCombo = (ComboBox)this.Controls[0].Controls["PortCombo"];
                ComboBox baudCombo = (ComboBox)this.Controls[0].Controls["BaudCombo"];
                string port = portCombo.SelectedItem?.ToString();
                int baudRate = (int)baudCombo.SelectedItem;

                if (string.IsNullOrEmpty(port))
                {
                    MessageBox.Show("Please select a serial port");
                    return;
                }

                serialPort = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One);
                serialPort.Open();
                isRunning = true;

                UpdateStatus("Connected to " + port);
                
                dataReadThread = new Thread(ReadData);
                dataReadThread.IsBackground = true;
                dataReadThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection error: " + ex.Message);
                isRunning = false;
            }
        }

        private void DisconnectDevice()
        {
            try
            {
                isRunning = false;
                if (serialPort?.IsOpen == true)
                {
                    serialPort.Close();
                    serialPort.Dispose();
                }
                UpdateStatus("Disconnected");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Disconnection error: " + ex.Message);
            }
        }

        private void ReadData()
        {
            while (isRunning && serialPort?.IsOpen == true)
            {
                try
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        string data = serialPort.ReadLine();
                        UpdateDisplay(data);
                    }
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    UpdateInfo("Error reading data: " + ex.Message);
                }
            }
        }

        private void UpdateDisplay(string data)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Label displayLabel = (Label)this.Controls[0].Controls["DisplayLabel"];
                displayLabel.Text = data;
                UpdateInfo($"[{DateTime.Now:HH:mm:ss}] {data}");
            });
        }

        private void UpdateStatus(string status)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Label statusLabel = (Label)this.Controls[0].Controls["StatusLabel"];
                statusLabel.Text = status;
            });
        }

        private void UpdateInfo(string info)
        {
            this.Invoke((MethodInvoker)delegate
            {
                TextBox infoText = (TextBox)this.Controls[0].Controls["InfoText"];
                infoText.AppendText(info + Environment.NewLine);
                infoText.ScrollToCaret();
            });
        }
    }
}
