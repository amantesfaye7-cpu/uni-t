using System;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Drawing;

namespace UniT161E
{
    public partial class MainForm : Form
    {
        private SerialPort? serialPort;
        private Thread? dataReadThread;
        private volatile bool isRunning = false;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "UNI-T 161E Multimeter Display";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            // Main panel
            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BackColor = Color.Black;
            this.Controls.Add(mainPanel);

            // Display label
            Label displayLabel = new Label();
            displayLabel.Name = "DisplayLabel";
            displayLabel.Text = "0.000";
            displayLabel.Font = new Font("Arial", 72, FontStyle.Bold);
            displayLabel.ForeColor = Color.Lime;
            displayLabel.TextAlign = ContentAlignment.MiddleCenter;
            displayLabel.Dock = DockStyle.Top;
            displayLabel.Height = 120;
            mainPanel.Controls.Add(displayLabel);

            // Status label
            Label statusLabel = new Label();
            statusLabel.Name = "StatusLabel";
            statusLabel.Text = "Disconnected";
            statusLabel.Font = new Font("Arial", 14);
            statusLabel.ForeColor = Color.White;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Height = 40;
            mainPanel.Controls.Add(statusLabel);

            // Control panel
            Panel controlPanel = new Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.Padding = new Padding(10);
            controlPanel.BackColor = Color.Black;
            mainPanel.Controls.Add(controlPanel);

            // Port selection
            Label portLabel = new Label();
            portLabel.Text = "Serial Port:";
            portLabel.ForeColor = Color.White;
            portLabel.Location = new Point(10, 10);
            portLabel.Size = new Size(100, 25);
            controlPanel.Controls.Add(portLabel);

            ComboBox portCombo = new ComboBox();
            portCombo.Name = "PortCombo";
            portCombo.Location = new Point(110, 10);
            portCombo.Size = new Size(150, 25);
            portCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            string[] ports = SerialPort.GetPortNames();
            portCombo.Items.AddRange(ports);
            if (ports.Length > 0) portCombo.SelectedIndex = 0;
            controlPanel.Controls.Add(portCombo);

            // Baud rate selection
            Label baudLabel = new Label();
            baudLabel.Text = "Baud Rate:";
            baudLabel.ForeColor = Color.White;
            baudLabel.Location = new Point(280, 10);
            baudLabel.Size = new Size(100, 25);
            controlPanel.Controls.Add(baudLabel);

            ComboBox baudCombo = new ComboBox();
            baudCombo.Name = "BaudCombo";
            baudCombo.Location = new Point(380, 10);
            baudCombo.Size = new Size(100, 25);
            baudCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            baudCombo.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            baudCombo.SelectedItem = 9600;
            controlPanel.Controls.Add(baudCombo);

            // Connect button
            Button connectButton = new Button();
            connectButton.Name = "ConnectButton";
            connectButton.Text = "Connect";
            connectButton.Location = new Point(490, 10);
            connectButton.Size = new Size(90, 25);
            connectButton.Click += ConnectButton_Click;
            controlPanel.Controls.Add(connectButton);

            // Info text
            TextBox infoText = new TextBox();
            infoText.Name = "InfoText";
            infoText.Location = new Point(10, 50);
            infoText.Size = new Size(570, 300);
            infoText.Multiline = true;
            infoText.ScrollBars = ScrollBars.Vertical;
            infoText.ReadOnly = true;
            infoText.BackColor = Color.Black;
            infoText.ForeColor = Color.Lime;
            infoText.Font = new Font("Courier New", 10);
            controlPanel.Controls.Add(infoText);
        }

        private void ConnectButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn)
                return;

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
                Panel? mainPanel = this.Controls[0] as Panel;
                if (mainPanel == null)
                    return;

                ComboBox? portCombo = mainPanel.Controls["PortCombo"] as ComboBox;
                ComboBox? baudCombo = mainPanel.Controls["BaudCombo"] as ComboBox;

                if (portCombo == null || baudCombo == null)
                    return;

                string? port = portCombo.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(port) || baudCombo.SelectedItem is not int baudRate)
                {
                    MessageBox.Show("Please select a serial port and valid baud rate");
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
                Panel? mainPanel = this.Controls[0] as Panel;
                if (mainPanel == null)
                    return;

                Label? displayLabel = mainPanel.Controls["DisplayLabel"] as Label;
                if (displayLabel != null)
                {
                    displayLabel.Text = data;
                    UpdateInfo($"[{DateTime.Now:HH:mm:ss}] {data}");
                }
            });
        }

        private void UpdateStatus(string status)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Panel? mainPanel = this.Controls[0] as Panel;
                if (mainPanel == null)
                    return;

                Label? statusLabel = mainPanel.Controls["StatusLabel"] as Label;
                if (statusLabel != null)
                {
                    statusLabel.Text = status;
                }
            });
        }

        private void UpdateInfo(string info)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Panel? mainPanel = this.Controls[0] as Panel;
                if (mainPanel == null)
                    return;

                TextBox? infoText = mainPanel.Controls["InfoText"] as TextBox;
                if (infoText != null)
                {
                    infoText.AppendText(info + Environment.NewLine);
                    infoText.ScrollToCaret();
                }
            });
        }
    }
}
