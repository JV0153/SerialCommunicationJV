using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SerialCommunication
{
    public partial class Form1 : Form
    {
        SerialPort serialPortArduino = new SerialPort()
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
        Timer timerOefening5 = new Timer();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();
                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);

                if (comboBoxPoort.Items.Count > 0)
                    comboBoxPoort.SelectedIndex = 0;

                comboBoxBaudrate.SelectedIndex = comboBoxBaudrate.Items.IndexOf("115200");

                timerOefening5.Interval = 1000;
                timerOefening5.Tick += timerOefening5_Tick;
                timerOefening5.Stop();
            }
            catch
            {
            }
        }

        private void cboPoort_DropDown(object sender, EventArgs e)
        {
            try
            {
                string selected = (string)comboBoxPoort.SelectedItem;
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();

                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);

                comboBoxPoort.SelectedIndex = comboBoxPoort.Items.IndexOf(selected);
            }
            catch
            {
                if (comboBoxPoort.Items.Count > 0)
                    comboBoxPoort.SelectedIndex = 0;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPortArduino.IsOpen)
                {
                    serialPortArduino.Close();

                    radioButtonVerbonden.Checked = false;
                    buttonConnect.Text = "Connect";
                    labelStatus.Text = "Status: Disconnected";
                }
                else
                {
                    serialPortArduino.PortName = (string)comboBoxPoort.SelectedItem;
                    serialPortArduino.BaudRate = Int32.Parse((string)comboBoxBaudrate.SelectedItem);
                    serialPortArduino.DataBits = (int)numericUpDownDatabits.Value;

                    if (radioButtonParityEven.Checked) serialPortArduino.Parity = Parity.Even;
                    else if (radioButtonParityOdd.Checked) serialPortArduino.Parity = Parity.Odd;
                    else if (radioButtonParityNone.Checked) serialPortArduino.Parity = Parity.None;
                    else if (radioButtonParityMark.Checked) serialPortArduino.Parity = Parity.Mark;
                    else if (radioButtonParitySpace.Checked) serialPortArduino.Parity = Parity.Space;

                    if (radioButtonStopbitsNone.Checked) serialPortArduino.StopBits = StopBits.None;
                    else if (radioButtonStopbitsOne.Checked) serialPortArduino.StopBits = StopBits.One;
                    else if (radioButtonStopbitsOnePointFive.Checked) serialPortArduino.StopBits = StopBits.OnePointFive;
                    else if (radioButtonStopbitsTwo.Checked) serialPortArduino.StopBits = StopBits.Two;

                    if (radioButtonHandshakeNone.Checked) serialPortArduino.Handshake = Handshake.None;
                    else if (radioButtonHandshakeRTS.Checked) serialPortArduino.Handshake = Handshake.RequestToSend;
                    else if (radioButtonHandshakeRTSXonXoff.Checked) serialPortArduino.Handshake = Handshake.RequestToSendXOnXOff;
                    else if (radioButtonHandshakeXonXoff.Checked) serialPortArduino.Handshake = Handshake.XOnXOff;

                    serialPortArduino.RtsEnable = checkBoxRtsEnable.Checked;
                    serialPortArduino.DtrEnable = checkBoxDtrEnable.Checked;

                    serialPortArduino.Open();

                    serialPortArduino.WriteLine("ping");
                    string antwoord = serialPortArduino.ReadLine().Trim();

                    if (antwoord == "pong")
                    {
                        radioButtonVerbonden.Checked = true;
                        buttonConnect.Text = "Disconnect";
                        labelStatus.Text = "Status: Connected";
                    }
                    else
                    {
                        serialPortArduino.Close();
                        labelStatus.Text = "Error: verkeerd antwoord";
                    }
                }
            }
            catch (Exception exception)
            {
                labelStatus.Text = "Error: " + exception.Message;

                if (serialPortArduino.IsOpen)
                    serialPortArduino.Close();

                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
            }
        }

        private int AnalogRead(int pin)
        {
            serialPortArduino.DiscardInBuffer();
            serialPortArduino.WriteLine("get a" + pin);

            string antwoord = serialPortArduino.ReadLine().Trim();

            // voorbeeld: "a1: 230"
            int dubbelpuntIndex = antwoord.IndexOf(':');

            if (dubbelpuntIndex != -1)
            {
                string getal = antwoord.Substring(dubbelpuntIndex + 1).Trim();

                if (int.TryParse(getal, out int waarde))
                {
                    return waarde;
                }
            }

            throw new Exception("Foute data: " + antwoord);
        }

        private void DigitalWrite(int pin, bool hoog)
        {
            if (hoog)
                serialPortArduino.WriteLine("set d" + pin + " 1");
            else
                serialPortArduino.WriteLine("set d" + pin + " 0");
        }

        private void timerOefening5_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!serialPortArduino.IsOpen)
                {
                    labelStatus.Text = "Status: Disconnected";
                    return;
                }

                int waardePot = AnalogRead(0);

                double ricoGewenst = (45.0 - 5.0) / 1023.0;
                double offsetGewenst = 5.0;
                double gewensteTemp = ricoGewenst * waardePot + offsetGewenst;

                labelGewensteTemp.Text = gewensteTemp.ToString("0.0") + " °C";

                int waardeLM35 = AnalogRead(1);

                double spanning = waardeLM35 * 5.0 / 1023.0;
                double huidigeTemp = spanning * 100.0;

                labelHuidigeTemp.Text = huidigeTemp.ToString("0.0") + " °C";

                if (huidigeTemp < gewensteTemp)
                {
                    DigitalWrite(2, true);
                }
                else
                {
                    DigitalWrite(2, false);
                }
            }
            catch (Exception exception)
            {
                timerOefening5.Stop();

                radioButtonVerbonden.Checked = serialPortArduino.IsOpen;
                labelStatus.Text = "Error tijdens werking: " + exception.Message;

                MessageBox.Show(exception.Message);
            }
        }
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabPageOefening5)
            {
                timerOefening5.Start();
            }
            else
            {
                timerOefening5.Stop();
            }
        }
    }
}