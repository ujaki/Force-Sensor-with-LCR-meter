using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;

namespace _1x3_Array_Force_Sensor
{
    public struct HeatPoint
    {
        public int X;
        public int Y;
        public byte Intensity;
        public HeatPoint(int iX, int iY, byte bIntensity)
        {
            X = iX;
            Y = iY;
            Intensity = bIntensity;
        }
    }

    public partial class Form1 : Form
    {
        private List<HeatPoint> HeatPoints = new List<HeatPoint>();

        private PictureBox picbs = new PictureBox();
        public int arr_Jet = 0;
        public int arr_init = 0;
        public int arr_row = 0;

        public MovingAverage ch1_MA = new MovingAverage();
        public MovingAverage ch2_MA = new MovingAverage();
        //-------------------------SiHo added-----------------
        private TcpClient LanSocket;   //create TCP socket
        private double timestamp = 0;  //시간 경과 체크용 변수
        private FolderBrowserDialog dialog = new FolderBrowserDialog();
        private string storePath = null; //파일 저장경로
        private DateTime datetime = DateTime.Now;  //acquires date and time
        private string newLine = System.Environment.NewLine; //\r\n
        //---------------------------------------------------------

        public Form1()
        {
            InitializeComponent();
            picbs = (PictureBox)this.Controls[("pictureBox" + 1).ToString()];
            arr_init = 0;
            arr_Jet = 0;
            setColorJet(0);
            DrawColorMap_Jet();
        }

        private Bitmap init_Bitmap(int n)
        {
            Bitmap bMap = new Bitmap(picbs.Width, picbs.Height);

            Graphics g = Graphics.FromImage(bMap);
            g.Clear(Color.White);

            return bMap;
        }

        private void EventHandler(object sender, EventArgs e)
        {
            //MessageBox.Show(sender.GetType().ToString());
            switch (sender.GetType().ToString())
            {
                case "System.Windows.Forms.Button":
                    OnButton(sender, e);
                    break;
                case "System.Windows.Forms.TextBox":
                    //  TextBox_Control(sender, e);
                    break;
            }
        }

        private void setColorJet(int val) {
            Bitmap bMap = new Bitmap(picbs.Width, picbs.Height);

            Graphics g = Graphics.FromImage(bMap);


            g.Clear(Jet_Color(val, 100));

            picbs.Image = bMap;

        }
        private void OnButton(object sender, EventArgs e)
        {
            Button btn = (Button)sender;

            int n = Convert.ToInt32(btn.Name.Substring(7)) - 1; // 0 ~

            Bitmap bMap = new Bitmap(picbs.Width, picbs.Height);

            init_Bitmap(n);
            
            Graphics g = Graphics.FromImage(bMap);
            arr_Jet++;
            if (arr_Jet >= 100) {
                arr_Jet = 100;
            }
            g.Clear(Jet_Color(arr_Jet, 100));

            picbs.Image = bMap;
        }

        private void DrawColorMap_Jet()
        {
            int hgt = colorBar.Height;
            int wid = colorBar.Width;

            Bitmap bMap = new Bitmap(wid, hgt);


            Graphics g = Graphics.FromImage(bMap);
            //Console.WriteLine(wid + ", " + hgt);

            for (int i = 0; i < hgt; i++)
            {
                using (Pen the_pen = new Pen(Jet_Color(i,hgt)))
                {
                    g.DrawLine(the_pen, 0, hgt-i, wid, hgt-i);
                }
            }
            colorBar.Image = bMap;
        }

        private Color Jet_Color(int n, int max)
        {
            double red = 0;
            double green = 0;
            double blue = 0;
            double t = 0;

            
            t = (n - (max/2)) / (double)(max/2);

            red = clamp(1.5 - Math.Abs(2.0 * t - 1.0));
            green = clamp(1.5 - Math.Abs(2.0 * t));
            blue = clamp(1.5 - Math.Abs(2.0 * t + 1.0));

            red *= 255;
            green *= 255;
            blue *= 255;


          //  Console.WriteLine(t + ", " + t + ":" + red + " " + green + " " + blue);

            return Color.FromArgb((byte)red, (byte)green, (byte)blue);

        }
        private double clamp(double v)
        {
            double t = (v < 0) ? 0 : v;
            return (t > 1.0) ? 1.0 : t;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (LanSocket.Connected) {
                LanSocket.Close();
            }
            if (timer1.Enabled)
            {
                timer1.Stop();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            arr_init = arr_row;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Start(); //타이머 시작
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SendMsg(":DISP ON");          //' Display : off
            timer1.Stop(); //측정 타이머 정지
            LanSocket.Close();
            button4.Text = "Connect";
            button4.BackColor = Color.Gray;
            button4.ForeColor = Color.Black;
            button4.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                int interval = 5;
                int.TryParse(textBox1.Text, out interval);
                timer1.Interval = interval;
                SetStart_LAN("192.168.0.1", 3500);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetStart_LAN(String hostName, UInt16 hostPort)
        {
            LanSocket = new TcpClient(hostName, hostPort);

            // SendMsg("*RST");
            SendMsg(":MODE LCR");          //' Mode : LCR
            SendMsg(":AVER 10");          //' Averaging : off
            SendMsg(":DISP OFF");          //' Display : off
                                           //Z : Impedance / Cs, Cp : Capacitance
                                           //for measuring Rp only
            SendMsg(":MEAS:ITEM 16,0");      //' Measurement Parameter: Z/ Y/ Phase/ Cs/ Cp/ D/ Ls/ Lp/  Q/ Rs/   G/  Rp/ X/ B/ RDC/ T/ OFF
                                            //                         1/ 2/     4/  8/ 16/  32/ 64/128/256/512/1024/2048/ ??? 이렇게 높이??
                                            //                                                       1    2    4    8   16  32  64   128  256
                                            //SendMsg(":MEAS:ITEM 16,0"); //for measuring Cp only

            SendMsg(":SPEE SLOW");
            SendMsg(":HEAD OFF");          //' Header: OFF
            SendMsg(":LEV V");            //' Signal level: Open-circuit voltage
            SendMsg(":LEV:VOLT 1");      // Signal level: 1V signal level
            SendMsg(":FREQ 1E3");        //' Measurement frequency: 1kHz
            SendMsg(":TRIG INT");        //' Trigger: External trigger
                                         // SendMsg(":TRIG:DEL 5");        //' Trigger: External trigger

            if (LanSocket.Connected)
            {
                button4.Text = "Connected";
                button4.BackColor = Color.Red;
                button4.ForeColor = Color.White;
                button4.Enabled = false;
                LanSocket.GetStream().Flush();
            }
                    
        }

        private void SendMsg(string strMsg)
        {
            byte[] SendBuffer;
            try
            {
                SendBuffer = Encoding.Default.GetBytes(strMsg + newLine);
                LanSocket.GetStream().Write(SendBuffer, 0, SendBuffer.Length); //Send message
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int length;
            byte[] ReceiveBuffer = new byte[40];
            try
            {
                SendMsg(":MEAS?");
                if (LanSocket.GetStream().DataAvailable)
                {
                    length = LanSocket.GetStream().Read(ReceiveBuffer, 0, 40);
                    string data = Encoding.Default.GetString(ReceiveBuffer, 0, length).Replace("\r\n", "");
                    textBox2.AppendText(data);

                    //데이터 파싱해서 표현하는 코드 <-- 이 부분 수정해야함
                    string temp_str = serialPort1.ReadLine();
                    //   Console.WriteLine(temp_str);

                    string[] arr = temp_str.Split(' ');     //, 단위로 구분해서 문자열 배열 형태로 저장 
                    int[] adc = new int[arr.Length];
                    //  Console.WriteLine(arr.Length);
                    for (int i = 0; i < 2; i++)
                    {
                        adc[i] = Convert.ToInt32(arr[i]);   //32bit 정수화
                        arr_row[i] = adc[i];
                        adc[i] -= arr_init;
                        adc[i] = adc[i] / 4;
                        if (adc[i] > 100) adc[i] = 100;
                    }
                    ch1_MA.ComputeAverage(adc[0]);
                    //ch2_MA.ComputeAverage(adc[1]);
                    setColorJet(ch1_MA.Average);
                    //setColorJet(1, ch2_MA.Average);
                    Console.WriteLine(adc[0] + " " + adc[1] + " ");
                    //-------------------------------------------------------
                }
                else return;


            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
