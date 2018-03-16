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
    /// <summary>
    /// CRC_B encoding This annex is provided for explanatory purposes and indicates
    /// the bit patterns that will exist in the physical layer. It is included for
    /// the purpose of checking an ISO/IEC 14443-3 Type B implementation of CRC_B
    /// encoding. Refer to ISO/IEC 3309 and CCITT X.25 2.2.7 and V.42 8.1.1.6.1 for
    /// further details. Initial Value = 'FFFF'
    /// </summary>
    public class CrcB
    {
        const ushort __crcBDefault = 0xffff;

        private static ushort UpdateCrc(byte b, ushort crc)
        {
            unchecked
            {
                byte ch = (byte)(b ^ (byte)(crc & 0x00ff));
                ch = (byte)(ch ^ (ch << 4));
                return (ushort)((crc >> 8) ^ (ch << 8) ^ (ch << 3) ^ (ch >> 4));
            }
        }

        /// <summary>
        /// Compute the checksum for a data block.
        /// </summary>
        /// <param name="bytes">Data block.</param>
        /// <returns>Checksum for data block.</returns>
        public static ushort ComputeCrc(byte[] bytes)
        {
            unchecked
            {
                var res = __crcBDefault;
                foreach (var b in bytes)
                    res = UpdateCrc(b, res);
                return (ushort)~res;
            }
        }
    }
    
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
        
        //AX.25
        byte[] AX25_TxHeader = { 'R' << 1, 'W' << 1, '3' << 1, 'W' << 1, 'W' << 1, 'W' << 1, 0x61, 
                                 'R' << 1, 'W' << 1, '3' << 1, 'W' << 1, 'W' << 1, 'W' << 1, 0x65, 
                                  0x03, 0xF0 };

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
                        //telemetry_format.SATTelemetry_t tlm = telemetry_format.fill_SATStruct(frame, 17);

                        richTextBox1.AppendText(call + "\r\n");

                       // string str_tlm = telemetry_format.SATStruct_str(tlm) + "\r\n";

                      //  richTextBox1.AppendText(str_tlm);

                      //  AddText(flog, str_tlm);

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

        private byte[] AX25frame(byte[] data)
        {
            //+16 байт заголовок, +2 байта crc
            byte[] frame = new byte[16 + data.Length];
            int i = 0;
            foreach (var d in AX25_TxHeader)
            {
                frame[i] = d;
                i++;
            }
            foreach (var d in data)
            {
                frame[i] = d;
                i++;
            }
          //  ushort crc = CrcB.ComputeCrc(frame);
          //  Array.Resize(ref frame, frame.Length + 2);
          //  frame[i] = (byte)(crc >> 8);
          //  frame[i + 1] = (byte)(crc);
            return frame;
        }

        //создает команду
        private byte[] Command(byte type, ushort param)
        {
            const byte gnd_uniqheader0	=	0x3C;
            const byte gnd_uniqheader1 =    0xA6;
            /*
             typedef struct {
		                   uint16_t	uniqheader;
		                   uint8_t cmdType;
		                   uint16_t cmdParam;
	        } gndcmd_t; 
             */
            byte[] cmd = new byte[5];
            cmd[0] = gnd_uniqheader1;
            cmd[1] = gnd_uniqheader0;
            cmd[2] = type;
            cmd[3] = (byte)(param >> 8);
            cmd[4] = (byte)(param);
            return cmd;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //отправить пакет
            byte[] data = Command(2,0);
            byte[] frame = AX25frame(data);
            byte[] tx_buf = new byte[frame.Length + 3];
            

            tx_buf[0] = FEND;
            tx_buf[1] = 0x00;
            int i = 2;
            foreach (var d in frame)
            {
                tx_buf[i] = d;
                i++;
            }
            tx_buf[i] = FEND;
            if (serialPort1.IsOpen)
            {
                     
                serialPort1.Write(tx_buf, 0, tx_buf.Length);
            }
            



        }
    }
}
