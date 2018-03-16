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
using System.Security.Cryptography;
 
using System.Threading;
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
        //телеметрия
        private const byte SAT_TLM_SIZE = 60;
        private const byte SAT_TLM_IMG_SIZE = 64;
        private const byte SAT_TLM_MINI_SIZE = 44;
        
       //AX.25
       byte[] AX25_TxHeader = { 'R' << 1, 'W' << 1, '3' << 1, 'W' << 1, 'W' << 1, 'W' << 1, 0x61, 
                                 'R' << 1, 'W' << 1, '3' << 1, 'W' << 1, 'W' << 1, 'W' << 1, 0x65, 
                                  0x03, 0xF0 };


       //AES
       //вектор инициализации
       static byte[] IV = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        static byte[] KEY = { 0x88, 0x5f, 0xab, 0x8f, 0xbb, 0xbc, 0xd7, 0x37, 0xae, 0x17, 0xf1, 0xd6, 0xcb, 0xfc, 0xcd, 0x99 };
        //приемный буфер
       private byte[] rxbuf = new byte[181136];
       private int rxhead = 0;
       private int rxtail = 0;
       private bool frame_start = false;
        Form2 frm;
       //private byte[] frame = new byte[512];
       string PortName;

        //телеметрия
        enum GROUND_cmd_t
        {
            gnd_cmd_get_status = 1,                 //запрос состояния
            gnd_cmd_get_fulltelemetry_nack,         //запрос телеметрии за виток без подтверждения приема пакетов
                                                    //(сбрасываются все пакеты последовательно)
            gnd_cmd_get_fulltelemetry_ack,          //запрос телеметрии за виток с подтверждением приема каждого пакета
                                                    //наземной станцией
            gnd_cmd_stop,                           //останов текущей передачи телеметрии
            gng_cmd_exp_enable,
            gng_cmd_exp_disable,
            gnd_cmd_get_image,                      //запрос картинки для передачи, cmdParam	номер картинки
        };
        // - Изображения
        public  byte[] rx_dat_buf = new byte[2048];
        public  byte[] rx_img_buf = new byte[2048];
        public  byte[] tx_dat_buf;
        public BinaryReader f_dat;
        public BinaryReader f_dat_;
        public FileStream fs;
        int img_rx_number = -1;
        string img_coment;
        byte[,] IMG;
        //log
        private const string path = @"log.csv";
       private FileStream flog;

       private static void AddText(FileStream fs, string value)
       {
           byte[] info = new UTF8Encoding(true).GetBytes(value);
           byte[] date = GetBytes(System.DateTime.Now.GetDateTimeFormats('G')[0] + ",");
           
           fs.Write(date, 0, date.Length);
           fs.Write(info, 0, info.Length);
           fs.Flush(true);
       }

       static byte[] GetBytes(string str)
       {
           byte[] bytes = new byte[str.Length * sizeof(char)];
           System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
           return bytes;
       }

       static string GetString(byte[] bytes)
       {
           char[] chars = new char[bytes.Length / sizeof(char)];
           System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
           return new string(chars);
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
                    serialPort1.BaudRate = 9600;
                    serialPort1.ReadTimeout = 1000;
                    serialPort1.Open();
                    serialPort1.DiscardInBuffer();
                    toolStripStatusLabel1.Text = "Порт " + PortName + "открыт";

                    //flog = File.Create(path);
                    flog = File.Open(path, FileMode.Append);
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
           frm = new Form2();
          

        }
        private Boolean all_not_z(byte[,] line,int y)
        {
            for (int i = 0; i < 60; i++)
                if (line[y,i] != 0) { return true;    }


            return false;

        }

        private void img_to_file(byte[,] img, BinaryWriter writer)
        {
            int i = 0;

        while( i<60000)
            {
                for (int j = 0; j < 60; j++)
                {

                    if ((IMG[i, j] == 0xff) && (j + 1 < 60) && (IMG[i, j + 1] == 0xd8))
                    {
                        writer.Write(IMG[i, j]);
                        j++;
                        writer.Write(IMG[i, j]);
                        
                        writer.Write(33612031); // ff e0 00 02
                        
                   
                         
                        Console.WriteLine("Вставка коментария\n\t\n\t\n\t\n\t");
                    }
                    else
                    if ((IMG[i, j] == 0xff) && (j + 2 < 60) && (IMG[i, j + 1] == 0xda) && (IMG[i, j + 2] == 0x8b))
                    {
                        writer.Write(IMG[i, j]);
                        j++;
                        writer.Write(IMG[i, j]);
                        j++;

                        Console.WriteLine("Исправка бага  \n\t\n\t\n\t\n\t");

                    }
                    else

                        writer.Write(IMG[i,j]);


                }



                
                i++;
            }
          


        }
  
        
        private void IMG_decod(byte [] block)
        {
           
            if (block[2]==0x00 && block[3]==0x00 && block[4] == 0xff && block[5] == 0xd8) // новый номер изображения
            {
                img_rx_number = block[0]; // сеанс приема определенного изображения
                // 
                IMG = new byte[65535,60];// подготовили и очистили буфер
                for (int i = 0; i < 65535; i++)
                    for (int j = 0; j < 60; j++)
                        IMG[i,j] = 0;

               

                int pos = ((block[3] << 8) + block[2]);  // определили позицию блока


                //for (int i = 0; i < 2; i++) IMG[pos, i] = block[i + 4]; // извлекли данные из блока
                // Костыль вставка блока коментария
                
                //
                for (int i = 0; i < 60; i++) IMG[pos,i] = block[i + 4]; // извлекли данные из блока

                using (BinaryWriter writer = new BinaryWriter(File.Open("img_"+img_rx_number + ".jpg", FileMode.OpenOrCreate)))
                {
                    img_to_file(IMG, writer);
                }
                frm.Show();
                frm.showIMG(img_rx_number + ".jpg");
                //frm.showIMG("img_" + img_rx_number + ".jpg");
                // IMG_decod_block(block, writer);



            } else
            if (block[0] == img_rx_number)
            {

                int pos = ((block[3] << 8) + block[2]);  // определили позицию блока
                Console.WriteLine("pos" + pos + "     " + block[2] + "     " + block[3]);
                for (int i = 0; i < 60; i++) IMG[pos, i] = block[i + 4]; // извлекли данные из блока
                try
                {

                    using (BinaryWriter writer = new BinaryWriter(File.Open(img_rx_number + ".jpg", FileMode.OpenOrCreate)))
                    {
                        img_to_file(IMG, writer);
                    }
                }
                catch {

                    using (BinaryWriter writer = new BinaryWriter(File.Open(img_rx_number + ".jpg", FileMode.OpenOrCreate)))
                    {
                        img_to_file(IMG, writer);
                    }

                }
               

            }
           
           



            using (BinaryWriter writer = new BinaryWriter(File.Open("test.jpg", FileMode.Append)))
        {
                writer.Write(FEND);
                writer.Write(block);
        }
        



        }




        int nr = 0;
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
                if (size > 16)
                {
                    if (frame[0] == DATA_FRAME)
                    {
                        //фрем данных
                        //выбираем позывной
                        string call = "From: ";
                        int i;
                        for (i = 8; i < 15; i++)
                        {

                            call += GetChr(frame[i] >> 1, 1);

                        }
                                                
                        call += " To: ";

                        for (i = 1; i < 8; i++)
                        {
                            call += GetChr(frame[i] >> 1, 1);
                        }

                        richTextBox1.AppendText(call + " size: " + size.ToString() +"\r\n");
                        Console.WriteLine("size" + (size));
                        //17 начало данных
                        if (size == (SAT_TLM_SIZE + 17))
                        {
                            telemetry_format.SATTelemetry_t tlm = telemetry_format.fill_SATStruct(frame, 17);
                            string str_tlm = "Norm," + telemetry_format.SATStruct_str(tlm) + "\r\n";
                            richTextBox1.AppendText(str_tlm);
                            AddText(flog, str_tlm);

                            filllabel(tlm);

                        }
                        else if (size == (SAT_TLM_MINI_SIZE + 17))
                        {
                            telemetry_format.SATTelemetry_mini_t tlm_mini = telemetry_format.fill_SATminiStruct(frame, 17);
                            string str_tlm = "Mini," + telemetry_format.SATminiStruct_str(tlm_mini) + "\r\n";
                            richTextBox1.AppendText(str_tlm);
                            AddText(flog, str_tlm);
                        }
                        else if (size == (81))
                        {
                          //  Console.WriteLine("/" + System.Text.Encoding.UTF8.GetString(frame));
                            //telemetry_format.SATTelemetry_mini_t tlm_mini = telemetry_format.fill_SATminiStruct(frame, 17);
                            string str_tlm = "IMG ," + frame.Length + "\r\n";

                            string result = str_tlm;

              
                            byte[] tmp=new byte [64]; 
                            for (int e=0;e<64;e++)
                            {
                                tmp[e] = frame[e+17];
                            }
                            nf++;
                            using (BinaryWriter writer = new BinaryWriter(File.Open("rx_dat_tmp.dat", FileMode.Append)))
                            {
                            writer.Write(tmp);
                            }

                            IMG_decod(tmp);

                            richTextBox1.AppendText(result);
                            AddText(flog, result);
                            Console.WriteLine("прием frame langth" +frame.Length);

                        }
                      //  Console.WriteLine("rx size" + (size));

                    }    
                }
                
            }


            
        }

        private void filllabel(telemetry_format.SATTelemetry_t tlm)
        {
            //ctrl
            label36.Text = tlm.CTRLTelemetry.MET.ToString();
            label35.Text = tlm.CTRLTelemetry.CTRL_MCU_temp.ToString();
            label45.Text = tlm.PWRTelemetry.OtherTelemetry.VBUS_I.ToString();
            //battery
            label8.Text = ((float)tlm.PWRTelemetry.BatteryTelemetry.U / 1000.0).ToString("0.00");
            label7.Text = ((float)tlm.PWRTelemetry.BatteryTelemetry.I_Ch / 1000).ToString("0.000");
            label6.Text = ((float)tlm.PWRTelemetry.BatteryTelemetry.I_Dis / 1000).ToString("0.000");
            label5.Text = tlm.PWRTelemetry.BatteryTelemetry.T.ToString();
            //panel temp
            label12.Text = tlm.PWRTelemetry.SolarPanelTemperature.T_XT.ToString();
            label11.Text = tlm.PWRTelemetry.SolarPanelTemperature.T_XB.ToString();
            label10.Text = tlm.PWRTelemetry.SolarPanelTemperature.T_YT.ToString();
            label9.Text = tlm.PWRTelemetry.SolarPanelTemperature.T_YB.ToString();
            label18.Text = tlm.PWRTelemetry.SolarPanelTemperature.T_ZT.ToString();
            label17.Text = tlm.PWRTelemetry.SolarPanelTemperature.T_ZB.ToString();
            //MPP
            label24.Text = ((float)tlm.PWRTelemetry.MPP1.U / 1000).ToString("0.00");
            label23.Text = ((float)tlm.PWRTelemetry.MPP1.I / 1000).ToString("0.00");

            label22.Text = ((float)tlm.PWRTelemetry.MPP2.U / 1000).ToString("0.00");
            label21.Text = ((float)tlm.PWRTelemetry.MPP2.I / 1000).ToString("0.00");

            label30.Text = ((float)tlm.PWRTelemetry.MPP3.U / 1000).ToString("0.00");
            label29.Text = ((float)tlm.PWRTelemetry.MPP3.I / 1000).ToString("0.00");
        }
        int kolframe = 0;

        int ttr = 0;

        int F_end = 0;   //  Конец  Фрейм в процессе извлечения, необходимо для извлечения  при разбитии буфером
        int F_start = 0; // Начало  Фрейм в процессе извлечения, необходимо для извлечения  при разбитии буфером
        byte[] frame = new byte[1024];
        int size = 0;

        int nf = 0;




        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //принимаем фрейм обрабленный знаками FEND, начинается с DATA_FRAME
            try
            {
                
                int count = serialPort1.BytesToRead;
                serialPort1.Read(rxbuf, rxtail, count);
                int len = rxtail + count;
                byte[] t_frame = new byte[254];
                byte t_frame_k = 0;


                Console.WriteLine("Поступили данные в COM:\n\t");
                Console.WriteLine("count= "+count+"; позиция в буфере: "+ rxtail+"; len= "+len+"\n\t");

                Console.WriteLine("Записали что пришло\n\t");
                ttr++;
                /*using (BinaryWriter writer = new BinaryWriter(File.Open("rx_buf_"+ttr+".dat", FileMode.Append)))
                {
                    for (int u = rxtail; u < len ; u++)
                        writer.Write(rxbuf[u]);

                  //  for (int u = rxtail; u < len + 1; u++)
                    //    writer.Write(0);

                }*/
                
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                int F_frame = 1; // первый запуск для участка буфера
                
                for (int u = rxtail; u < len ; u++)
                {
                    if ((F_start==0) /*&& (F_end==0)*/) // Если начало буфера и ранее небыло незавершенной записи фрейма
                    {
                        if (rxbuf[u] == FEND && ((u+1)<len) && (rxbuf[u+1]== DATA_FRAME))  // Если нашли начало фрейма  C0 00
                        {
                            F_start = 1; // Флаг начала записи фрейма (текушее значение С0 опускаем )
                            t_frame_k = 0; // Указатель на позицию временного фрейма
                            size = 0; // Указатель на позицию в декодированом фрейме
                        }


                    }

                    else


                    if (/*(F_start == 1) && (F_end == 0)*/true) // В мы  процессе записи фрейма и коней не найден
                    {
                        if (rxbuf[u] == FEND )  // Если нашли конец  C0 
                        {
                            F_start = 0; // Флаг начала записи фрейма (текушее значение С0 опускаем )
                                         /**/
                           // Декодируем KISS
                           
                            for (int i=0; i<t_frame_k;) // пока не переберем временный фрейм
                            {
                                if (t_frame[i] == FESC)
                                {
                                    if (t_frame[i+1] == TFEND)
                                    {
                                        frame[size] = FEND;
                                        i+= 2;
                                        size++;
                                      //  Console.WriteLine("KISS (FECS & TFEND ) >> FEND");
                                    }
                                    else if (t_frame[i+1] == TFESC)
                                    {
                                        frame[size] = FESC;
                                        i += 2;
                                        size++;
                                       // Console.WriteLine("KISS (FECS & TFESC ) >> FESC");
                                    }
                                }
                                else
                                {
                                    frame[size] = t_frame[i];
                                    i += 1;
                                    size++;
                                }
                                
                            } // Декодировали KISS  frame - чистый ;
                            nf++;
                           /* using (BinaryWriter writer = new BinaryWriter(File.Open("rx_frame_" + nf + ".dat", FileMode.Append)))
                            {
                                for (int y= 0; y< size; y++)
                                    writer.Write(frame[y]);
                              
                            }
                            using (BinaryWriter writer = new BinaryWriter(File.Open("rx_t_frame_" + nf + ".dat", FileMode.Append)))
                            {
                                for (int y = 0; y < t_frame_k; y++)
                                    writer.Write(t_frame[y]);
                            }*/
                            frame_Decode(frame, size);/* внимание */


                            /**/
                        } else
                        {
                            // ОК это не конец и не начало =>  пишем 
                            t_frame[t_frame_k] = rxbuf[u];
                            t_frame_k++;
                        }


                    }

                }
                F_frame = 0;// Мы прошли буфепр






                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                for (int i = rxtail; /*i < len*/false; i++)
                {
                    if (rxbuf[i] == FEND) 
                    {
                        if (!frame_start)
                        {
                            //нашли начало фрейма
                            rxhead = rxtail + i;
                            Console.WriteLine("Нашли начало фрейма:" + "pos(i)= " + i+ " Записали на позицию:"+ rxhead + "\n\t");
                            frame_start = true;
                        }
                        else
                        {
                            if (rxhead != rxtail)
                            {
                            
                                Console.WriteLine("Приняли весь фрейм" + "размер : " +(rxtail - rxhead) + "\n\t");
                                //приняли весь фрейм
                                byte[] frame = new byte[rxtail - rxhead ];
                                int size = 0;
                                //for (int j = rxhead + 1; j < rxtail + 1; j++)
                                int j = rxhead + 1;

                                Console.WriteLine("Цикл  : " +j+" to "+ rxtail + " \n\t");
                                while (j < rxtail + 1)
                                {
                                    if (rxbuf[j] == FESC)
                                    {
                                        if (rxbuf[j + 1] == TFEND)
                                        {
                                            frame[size] = FEND;
                                            j += 2;
                                            Console.WriteLine("KISS (FECS & TFEND ) >> FEND");
                                        }
                                        else if (rxbuf[j + 1] == TFESC)
                                        {
                                            frame[size] = FESC;
                                            j += 2;
                                            Console.WriteLine("KISS (FECS & TFESC ) >> FESC");
                                        }
                                    }
                                    else
                                    {
                                        frame[size] = rxbuf[j];
                                        j += 1;
                                    }
                                    Console.WriteLine("- "+ frame[size]);
                                    size += 1;
                                }

                                rxhead = rxtail;

                                Console.WriteLine("Конец \n\t" );

                                nf++;
                                using (BinaryWriter writer = new BinaryWriter(File.Open("rx_frame_"+nf+".dat", FileMode.Append)))
                                {
                                     
                                    writer.Write(frame);
                                }
                              

                                 frame_Decode(frame, size);/* внимание */





                                //  IMG_decod(frame);
                                kolframe++;
                                Console.WriteLine("j="+j+"fsaze:  " + (size)+"  kolvof: "+ kolframe+"frame . size"+frame.Length+"\n\t");
                                if (rxtail >= len - 2)
                                {
                                    //конец буфера
                                    rxhead = 0;
                                    rxtail = 0;
                                  /*  foreach (byte u in rxbuf)
                                    {
                                        rxbuf[u] = 0;
                                        

                                    }*/
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
                //toolStripStatusLabel1.Text = "Ошибка при работе с портом " + PortName;
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
            cmd[3] = (byte)(param);
            cmd[4] = (byte)(param >> 8);
            return cmd;

        }

        private byte[] Command_Enc(byte type, ushort param)
        {
            const byte gnd_uniqheader0 = 0x3C;
            const byte gnd_uniqheader1 = 0xA6;

            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            /*
             typedef struct {
		                   uint16_t	uniqheader;
		                   uint8_t cmdType;
		                   uint16_t cmdParam;
	        } gndcmd_t; 
             */
            byte[] cmd = new byte[16];
            rngCsp.GetBytes(cmd);

            cmd[0] = gnd_uniqheader1;
            cmd[1] = gnd_uniqheader0;
            cmd[2] = type;
            cmd[3] = (byte)(param);
            cmd[4] = (byte)(param >> 8);
            return AESEncryptBytes (cmd);

        }

      /*  private byte[] IMG_Enc(char[] buf)
        {
           /* const byte gnd_uniqheader0 = 0x3C;
            const byte gnd_uniqheader1 = 0xA6;

            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
      
            buf= new byte[64];
            rngCsp.GetBytes(cmd);

            cmd[0] = gnd_uniqheader1;
            cmd[1] = gnd_uniqheader0;
            cmd[2] = type;
            cmd[3] = (byte)(param);
            cmd[4] = (byte)(param >> 8);
            return AESEncryptBytes(cmd); 

        }*/
        private void button2_Click(object sender, EventArgs e)
        {
            //отправить пакет
            //byte[] data = Command(7,2);

            byte[] data;
            byte[] frame;
            byte[] tx_buf;
      
            if (comboBox2.SelectedIndex<7)
            {
                Console.WriteLine("comboBox1.SelectedIndex<7"+ comboBox2.SelectedIndex);
                data = Command_Enc((byte)comboBox2.SelectedIndex, 0);
                frame = AX25frame(data);
                tx_buf = new byte[frame.Length + 3];


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

            } else
            if (comboBox2.SelectedIndex ==7)
            {
                Console.WriteLine("comboBox1.SelectedIndex=7\n\t");
                //char[] tmp = new char[32];
               // Encoding enc= Encoding.Unicode;
                int y = 0;

                /* 
                
                // Set Position to the beginning of the stream.
                binReader.BaseStream.Position = 0;

                // Read and verify the data.
                byte[] verifyArray = binReader.ReadBytes(arrayLength);
                if (verifyArray.Length != arrayLength)
                {
                    Console.WriteLine("Error writing the data.");
                    return;
                }


                /**/
                data = f_dat.ReadBytes(64);
                Console.WriteLine("Читаем файл по 64 байта\n\t"+ data.Length + "\n\t");
                while (data.Length==64)
                {
                    y++;
                                 
                   
                    frame = AX25frame(data);
                    tx_buf = new byte[2040];


                    Console.WriteLine("Обрамили данные в ах25 дописываем в файл rx_frame" + frame.Length + "\n\t");


                    using (BinaryWriter writer = new BinaryWriter(File.Open("tx_frame.dat", FileMode.Append)))
                    {

                        writer.Write(frame);
                    }


                    tx_buf[0] = FEND;
                    tx_buf[1] = 0x00;
                    int i = 2;
                    Console.WriteLine("Делаем замены в данных \n\t");
                    foreach (var d in frame)
                                   {
                                            if (d == FEND)
                                            {
                                                tx_buf[i] = FESC;
                                                i++;
                                                tx_buf[i] = TFEND;
                                                i++;
                                                Console.WriteLine("KISS (FESC & TFEND ) << FEND");
                                            }
                                            else if (d == FESC)
                                            {
                                                tx_buf[i] = FESC;
                                                i++;
                                                tx_buf[i] = TFESC;
                                                i++;
                                                Console.WriteLine("KISS (FECS & TFESC ) << FESC");
                                            }
                                            else
                                            {
                                                tx_buf[i] = d;
                                                i++;
                                            }
                                    }

                    Console.WriteLine("Замены сделаны KISS my \n\t пишем буфер в файл tx_buf");
                    tx_buf[i] = FEND;
                    using (BinaryWriter writer = new BinaryWriter(File.Open("tx_buf.dat", FileMode.Append)))
                    {
                       for (int u=0;u<i+1; u++)
                             writer.Write(tx_buf[u]);
                    }


                    if (serialPort1.IsOpen)
                                    {
                        
                                    serialPort1.Write(tx_buf, 0, i+1);
                        Console.WriteLine("length Com" + i);
                        Console.WriteLine("kol: " + y + "      " + y * 64+"i  =  "+i+1);
                     //   IMG_decod(tx_buf);
                    }
                    Thread.Sleep(13);
                    data = f_dat.ReadBytes(64);
                    
                }

                Console.WriteLine(" end tx kol: " +y+"      "+ y*64);
            };

        }

        private void label27_Click(object sender, EventArgs e)
        {

        }

        private void groupBox7_Enter(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
          
        }

        //шиврование
        private byte[] AESEncryptBytes(byte[] clearBytes)
        {
            byte[] encryptedBytes = null;

            // create an AES object
            using (Aes aes = new AesManaged())
            {
                // set the key size to 128
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Key = KEY;
                aes.IV = IV;
                aes.Padding = PaddingMode.None;
                aes.Mode = CipherMode.ECB;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }
            return encryptedBytes;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                    {
               } 

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
           // Stream myStream = null;

            if (comboBox2.SelectedIndex==7)
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Console.WriteLine(openFileDialog1.FileName);
                    try
                    {
                        fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read);

                        f_dat = new BinaryReader(fs, new ASCIIEncoding());

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                    }
                }

            }
                ;
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
          
            frm.showIMG(img_rx_number+".jpg");
        }
    }

}
