using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Telemetry;

namespace Telemetry
{
    public partial class Form1 : Form
    {
       //KISS
       private const byte FEND = 0xC0;
       private const byte FESC = (0xDB);
       private const byte TFEND = (0xDC);
       private const byte TFESC = (0xDD);

       private const byte  DATA_FRAME = (0x00);
       private const byte  TX_DELAY = (0x01);
       private const byte  PERSISTENCE = (0x02);
       private const byte  SLOT_TIME = (0x03);
       private const byte  TX_TAIL = (0x04);
       private const byte  FULL_DUPLEX = (0x05);
       private const byte  SET_HARDWARE = (0x06);
       private const byte  RETURN = (0xFF);

        //приемный буфер
       private byte[] rxbuf = new byte[2048];
       private int rxhead = 0;
       private int rxtail = 0;
       private bool frame_start = false;

       //private byte[] frame = new byte[512];
       string PortName;

        //телеметрия
      
        //log
       private const string path = @"log.csv";
       private FileStream flog;

       private static void AddText(FileStream fs, string value)
       {
           byte[] info = new UTF8Encoding(true).GetBytes(value);
           fs.Write(info, 0, info.Length);
       }

       public char GetChr(int code, int codepage)
       {
           // Check
           if (code < -32768 || code > 65535) throw new ArgumentException("Out of range", "code");

           // ASCII
           if (code > -1 && code < 128) return Convert.ToChar(code);

           // Get encoding
           var encoding = Encoding.GetEncoding(codepage);

           // Check
           if (encoding.IsSingleByte && (code < 0 || code > 255)) throw new ArgumentException("Out of range for codepage", "code");

           // Get decoder
           var decoder = encoding.GetDecoder();

           // Create bytes
           byte[] bytes;
           if (code > -1 && code < 256) bytes = new byte[] { (byte)(code & 255) };
           else bytes = new byte[] { (byte)((code & 65280) >> 8), (byte)(code & 255) };

           // Get chars
           char[] chars = new char[2];
           decoder.GetChars(bytes, 0, bytes.Length, chars, 0);

           // Return
           return chars[0];
       }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            

            if (!serialPort1.IsOpen)
            {
                PortName = comboBox1.Text;
                try
                {
                    serialPort1.PortName = PortName;
                    serialPort1.BaudRate = 115200;
                    serialPort1.ReadTimeout = 1000;
                    serialPort1.Open();
                    serialPort1.DiscardInBuffer();
                    toolStripStatusLabel1.Text = "Порт " + PortName + "открыт";

                    flog = File.Create(path);
                }
                catch (Exception)
                {
                    toolStripStatusLabel1.Text = "Не удалось открыть порт " + PortName;
                    throw;
                }     
            }
           

 
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (string pname in System.IO.Ports.SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(pname);
            }

            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;

            
            
        }

        delegate void frame_Decode_Callback(byte[] frame, int size);
        private void frame_Decode(byte []frame, int size)
        {
            if (this.InvokeRequired)
            {
                frame_Decode_Callback D = new frame_Decode_Callback(frame_Decode);
                this.Invoke(D, new object[] { frame, size });
            }
            else
            {
                //разбираем фрейм
                if (frame.Length > 16)
                {
                    if (frame[0] == DATA_FRAME)
                    {
                        //фрем данных
                        //выбираем позывной
                        string call = "From: ";
                        int i;

                        for (i = 1; i < 8; i++)
                        {
                            call += GetChr(frame[i] >> 1, 1);
                        }
                        call += " To: ";
                        for (i = 8; i < 15; i++)
                        {

                            call += GetChr(frame[i] >> 1, 1);

                        }

                        //17 начало данных
                        telemetry_format.SATTelemetry_t tlm = telemetry_format.fill_SATStruct(frame, 17);

                        richTextBox1.AppendText(call + "\r\n");

                        string str_tlm = telemetry_format.SATStruct_str(tlm) + "\r\n";

                        richTextBox1.AppendText(str_tlm);

                        AddText(flog, str_tlm);

                    }    
                }
                
            }


            
        }
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //принимаем фрейм обрабленный знаками FEND, начинается с DATA_FRAME
            try
            {
                int count = serialPort1.BytesToRead;
                serialPort1.Read(rxbuf, rxtail, count);
                int len = rxtail + count;
                for (int i = rxtail; i < len; i++)
                {
                    if (rxbuf[i] == FEND) 
                    {
                        if (!frame_start)
                        {
                            //нашли начало фрейма
                            rxhead = rxtail + i;
                           
                            frame_start = true;
                        }
                        else
                        {
                            if (rxhead != rxtail)
                            {
                                //приняли весь фрейм
                                byte[] frame = new byte[rxtail - rxhead ];
                                int size = 0;
                                //for (int j = rxhead + 1; j < rxtail + 1; j++)
                                int j = rxhead + 1;
                                while (j < rxtail + 1)
                                {
                                    if (rxbuf[j] == FESC)
                                    {
                                        if (rxbuf[j + 1] == TFEND)
                                        {
                                            frame[size] = FEND;
                                            j += 2;
                                        }
                                        else if (rxbuf[j + 1] == TFESC)
                                        {
                                            frame[size] = FESC;
                                            j += 2;
                                        }
                                    }
                                    else
                                    {
                                        frame[size] = rxbuf[j];
                                        j += 1;
                                    }
                                    size += 1;
                                }
                                rxhead = rxtail;
                                frame_Decode(frame, size);
                                if (rxtail >= len - 2)
                                {
                                    //конец буфера
                                    rxhead = 0;
                                    rxtail = 0;
                                }
                          
                            }
                        }
                        
                    }
                    else
                    {
                        if (frame_start)
                        {
                             rxtail = rxtail + 1;
                        }
                    }
                    
                }

            }
            catch (Exception)
            {
                toolStripStatusLabel1.Text = "Ошибка при работе с портом " + PortName;
                throw;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                flog.Close();
            }
        }
    }
}
