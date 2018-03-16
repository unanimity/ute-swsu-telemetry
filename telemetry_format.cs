using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telemetry
{
    using uint16_t = UInt16;
    using int16_t = Int16;
    using uint8_t = Byte;
    using int8_t = SByte;
    using uint32_t = UInt32;
    using int32_t = Int32;
    



    class telemetry_format
    {
        public struct IMGdataTelemetry_t
        {
            public uint16_t img_number;		// номер изображения
            public uint16_t blk_number;     // номер блока изображения
            public uint8_t [] data;
        };
        public struct IMGtxtTelemetry_t
        {
            public uint16_t[] txt;

        };

        public struct MPPTelemetry_t
        {
            public uint16_t U;		//mV
            public uint16_t I;		//mA
           
        } ;

        public struct SolarPanelTemperature_t
        {
            public int8_t T_XT;
            public int8_t T_XB;
            public int8_t T_YT;
            public int8_t T_YB;
            public int8_t T_ZT;
            public int8_t T_ZB;
        } ;

        public struct BatteryTelemetry_t  
        {
            public uint16_t U;
            public uint16_t I_Ch;
            public uint16_t I_Dis;
            public int8_t T;
            public int32_t Pwr_balance;
        } ;
        
        public struct PWRState_t
        {
	      //  uint8_t		en_MPP:1;
	      //  uint8_t		en_5V:1;
	      //  uint8_t		en_3V3:1;
            public uint8_t state;
        } ;



        public struct OtherTelemetry_t
        {
            public uint16_t VBUS_I;
            public int8_t MCU_T;	
        } ;

        public	struct PWRTelemetry_t
        {
            public MPPTelemetry_t MPP1;
            public MPPTelemetry_t MPP2;
            public MPPTelemetry_t MPP3;
            public SolarPanelTemperature_t SolarPanelTemperature;
            public BatteryTelemetry_t BatteryTelemetry;
            public OtherTelemetry_t OtherTelemetry;
            public PWRState_t PWRState;
        } ;

        public const uint16_t pwr_uniq_header = 0xFE14;

        public struct PWRTelemetry_packet_t
        {
            public uint16_t header;
            public uint8_t size;
            public PWRTelemetry_t PWRTelemetry;
            public uint8_t crc8;
        } ;

        public struct COMTelemetry_t
        {
	
	        //uint8_t	PA_level;
            public int16_t RSSI_level;
            public int8_t COM_MCU_temp;
            public int8_t ADF_temp;
	
        } ;

        public struct CTRLTelemetry_t
        {
            public uint32_t MET;	//mission elapsed time
            public uint8_t EXPStat;
            public int8_t CTRL_MCU_temp;
        } ;

        public struct EXPbinTelemetry_t
        {
            public uint16_t Telemetry_bin_Hv_current;     //потребление тока от батареи, ADC
            public uint16_t Telemetry_bin_tok_magnitude;		//целая часть
            public uint8_t Telemetry_bin_tok_exponent;		//сколько нулей
            public uint16_t Telemetry_bin_ion_tok_magnitude;		//целая часть
            public uint8_t Telemetry_bin_ion_tok_exponent;		//сколько нулей
            public uint16_t Telemetry_bin_adc;		//ток
            public uint8_t Telemetry_bin_gain_adc;	//усиление ацп
            public uint8_t Telemetry_bin_gain_ui;	//ток-напряжение (какой резистор)
            // 0 1к
            // 1 100к
            // 2 10М
            // 3 100М
            public uint16_t Telemetry_bin_Temp;    //температура, ADC
            public uint16_t Telemetry_bin_Hi_volt;    //измеренное значение высокого напряжения, ADC
            public uint8_t Telemetry_bin_status;
        };

        public struct EXPTelemetry_mini_t
        {
            public uint16_t Telemetry_bin_adc;		//ток
            public uint8_t Telemetry_bin_gain_adc;	//усиление ацп
            public uint8_t Telemetry_bin_gain_ui;	//ток-напряжение (какой резистор)
            public uint8_t Telemetry_bin_status;
        };

        public struct SATTelemetry_t
        {
            public COMTelemetry_t COMTelemetry;
            public CTRLTelemetry_t CTRLTelemetry;
            public PWRTelemetry_t PWRTelemetry;
            public EXPbinTelemetry_t EXPbinTelemetry;
        } ;

        public struct SATTelemetry_mini_t
        {
            //public COMTelemetry_t COMTelemetry;
            public CTRLTelemetry_t CTRLTelemetry;
            public PWRTelemetry_t PWRTelemetry;
            public EXPTelemetry_mini_t EXPTelemetry_mini;
        } ;

        public static SATTelemetry_t fill_SATStruct(byte[] src, int offset)
        {
            SATTelemetry_t tlm = new SATTelemetry_t();

            tlm.COMTelemetry.RSSI_level = (int16_t)((src[offset + 1]<<8 | src[offset + 0]));
            tlm.COMTelemetry.COM_MCU_temp = (int8_t)src[offset + 2];
            tlm.COMTelemetry.ADF_temp = (int8_t)src[offset + 3];

            tlm.CTRLTelemetry.MET = (uint32_t)((src[offset + 7] << 24) | (src[offset + 6] << 16) | (src[offset + 5] << 8) | (src[offset + 4] << 0));
            tlm.CTRLTelemetry.EXPStat = (uint8_t)src[offset + 8];
            tlm.CTRLTelemetry.CTRL_MCU_temp = (int8_t)src[offset + 9];

            tlm.PWRTelemetry.MPP1.U = (uint16_t)((uint16_t)(src[offset + 11] * (uint16_t)256) + (uint16_t)src[offset + 10]);
            tlm.PWRTelemetry.MPP1.I = (uint16_t)((uint16_t)(src[offset + 13] * (uint16_t)256) + (uint16_t)src[offset + 12]);
           
            tlm.PWRTelemetry.MPP2.U = (uint16_t)((uint16_t)(src[offset + 15] * (uint16_t)256) + (uint16_t)src[offset + 14]);
            tlm.PWRTelemetry.MPP2.I = (uint16_t)((uint16_t)(src[offset + 17] * (uint16_t)256) + (uint16_t)src[offset + 16]);
           
            tlm.PWRTelemetry.MPP3.U = (uint16_t)((uint16_t)(src[offset + 19] * (uint16_t)256) + (uint16_t)src[offset + 18]);
            tlm.PWRTelemetry.MPP3.I = (uint16_t)((uint16_t)(src[offset + 21] * (uint16_t)256) + (uint16_t)src[offset + 20]);

            tlm.PWRTelemetry.SolarPanelTemperature.T_XT = (int8_t)src[offset + 22];
            tlm.PWRTelemetry.SolarPanelTemperature.T_XB = (int8_t)src[offset + 23];
            tlm.PWRTelemetry.SolarPanelTemperature.T_YT = (int8_t)src[offset + 24];
            tlm.PWRTelemetry.SolarPanelTemperature.T_YB = (int8_t)src[offset + 25];
            tlm.PWRTelemetry.SolarPanelTemperature.T_ZT = (int8_t)src[offset + 26];
            tlm.PWRTelemetry.SolarPanelTemperature.T_ZB = (int8_t)src[offset + 27];
           
            tlm.PWRTelemetry.BatteryTelemetry.U = (uint16_t)((uint16_t)(src[offset + 29] * (uint16_t)256) + (uint16_t)src[offset + 28]);
            tlm.PWRTelemetry.BatteryTelemetry.I_Ch = (uint16_t)((uint16_t)(src[offset + 31] * (uint16_t)256) + (uint16_t)src[offset + 30]);
            tlm.PWRTelemetry.BatteryTelemetry.I_Dis = (uint16_t)((uint16_t)(src[offset + 33] * (uint16_t)256) + (uint16_t)src[offset + 32]);
            tlm.PWRTelemetry.BatteryTelemetry.T = (int8_t)src[offset + 34];
            tlm.PWRTelemetry.BatteryTelemetry.Pwr_balance = (src[offset + 38] << 24) | (src[offset + 37] << 16) | (src[offset + 36] << 8) | (src[offset + 35] << 0);

            tlm.PWRTelemetry.OtherTelemetry.VBUS_I = (uint16_t)((uint16_t)(src[offset + 40] * (uint16_t)256) + (uint16_t)src[offset + 39]);
            tlm.PWRTelemetry.OtherTelemetry.MCU_T = (int8_t)src[offset + 41];

            tlm.PWRTelemetry.PWRState.state = (uint8_t)src[offset + 42];

            tlm.EXPbinTelemetry.Telemetry_bin_Hv_current = (uint16_t)((uint16_t)(src[offset + 44] * (uint16_t)256) + (uint16_t)src[offset + 43]);
            tlm.EXPbinTelemetry.Telemetry_bin_tok_magnitude = (uint16_t)((uint16_t)(src[offset + 46] * (uint16_t)256) + (uint16_t)src[offset + 45]);
            tlm.EXPbinTelemetry.Telemetry_bin_Hv_current = (uint8_t)src[offset + 47];
            tlm.EXPbinTelemetry.Telemetry_bin_ion_tok_magnitude = (uint16_t)((uint16_t)(src[offset + 49] * (uint16_t)256) + (uint16_t)src[offset + 48]);
            tlm.EXPbinTelemetry.Telemetry_bin_ion_tok_exponent = (uint8_t)src[offset + 50];
            tlm.EXPbinTelemetry.Telemetry_bin_adc = (uint16_t)((uint16_t)(src[offset + 52] * (uint16_t)256) + (uint16_t)src[offset + 51]);
            tlm.EXPbinTelemetry.Telemetry_bin_gain_adc = (uint8_t)src[offset + 53];
            tlm.EXPbinTelemetry.Telemetry_bin_gain_ui = (uint8_t)src[offset + 54];
            tlm.EXPbinTelemetry.Telemetry_bin_Temp = (uint16_t)((uint16_t)(src[offset + 56] * (uint16_t)256) + (uint16_t)src[offset + 55]);
            tlm.EXPbinTelemetry.Telemetry_bin_Hi_volt = (uint16_t)((uint16_t)(src[offset + 58] * (uint16_t)256) + (uint16_t)src[offset + 57]);
            tlm.EXPbinTelemetry.Telemetry_bin_status = (uint8_t)src[offset + 59];

            return tlm;
        }

        public static SATTelemetry_mini_t fill_SATminiStruct(byte[] src, int offset)
        {
            SATTelemetry_mini_t tlm = new SATTelemetry_mini_t();

            
            tlm.CTRLTelemetry.MET = (uint32_t)((src[offset + 3] << 24) | (src[offset + 2] << 16) | (src[offset + 1] << 8) | (src[offset + 0] << 0));
            tlm.CTRLTelemetry.EXPStat = (uint8_t)src[offset + 4];
            tlm.CTRLTelemetry.CTRL_MCU_temp = (int8_t)src[offset + 5];

            tlm.PWRTelemetry.MPP1.U = (uint16_t)((uint16_t)(src[offset + 7] * (uint16_t)256) + (uint16_t)src[offset + 6]);
            tlm.PWRTelemetry.MPP1.I = (uint16_t)((uint16_t)(src[offset + 9] * (uint16_t)256) + (uint16_t)src[offset + 8]);

            tlm.PWRTelemetry.MPP2.U = (uint16_t)((uint16_t)(src[offset + 11] * (uint16_t)256) + (uint16_t)src[offset + 10]);
            tlm.PWRTelemetry.MPP2.I = (uint16_t)((uint16_t)(src[offset + 13] * (uint16_t)256) + (uint16_t)src[offset + 12]);

            tlm.PWRTelemetry.MPP3.U = (uint16_t)((uint16_t)(src[offset + 15] * (uint16_t)256) + (uint16_t)src[offset + 14]);
            tlm.PWRTelemetry.MPP3.I = (uint16_t)((uint16_t)(src[offset + 17] * (uint16_t)256) + (uint16_t)src[offset + 16]);

            tlm.PWRTelemetry.SolarPanelTemperature.T_XT = (int8_t)src[offset + 18];
            tlm.PWRTelemetry.SolarPanelTemperature.T_XB = (int8_t)src[offset + 19];
            tlm.PWRTelemetry.SolarPanelTemperature.T_YT = (int8_t)src[offset + 20];
            tlm.PWRTelemetry.SolarPanelTemperature.T_YB = (int8_t)src[offset + 21];
            tlm.PWRTelemetry.SolarPanelTemperature.T_ZT = (int8_t)src[offset + 22];
            tlm.PWRTelemetry.SolarPanelTemperature.T_ZB = (int8_t)src[offset + 23];

            tlm.PWRTelemetry.BatteryTelemetry.U = (uint16_t)((uint16_t)(src[offset + 25] * (uint16_t)256) + (uint16_t)src[offset + 24]);
            tlm.PWRTelemetry.BatteryTelemetry.I_Ch = (uint16_t)((uint16_t)(src[offset + 27] * (uint16_t)256) + (uint16_t)src[offset + 26]);
            tlm.PWRTelemetry.BatteryTelemetry.I_Dis = (uint16_t)((uint16_t)(src[offset + 29] * (uint16_t)256) + (uint16_t)src[offset + 28]);
            tlm.PWRTelemetry.BatteryTelemetry.T = (int8_t)src[offset + 30];
            tlm.PWRTelemetry.BatteryTelemetry.Pwr_balance = (src[offset + 34] << 24) | (src[offset + 33] << 16) | (src[offset + 32] << 8) | (src[offset + 31] << 0);

            tlm.PWRTelemetry.OtherTelemetry.VBUS_I = (uint16_t)((uint16_t)(src[offset + 36] * (uint16_t)256) + (uint16_t)src[offset + 35]);
            tlm.PWRTelemetry.OtherTelemetry.MCU_T = (int8_t)src[offset + 37];

            tlm.PWRTelemetry.PWRState.state = (uint8_t)src[offset + 38];

            
            tlm.EXPTelemetry_mini.Telemetry_bin_adc = (uint16_t)((uint16_t)(src[offset + 40] * (uint16_t)256) + (uint16_t)src[offset + 39]);
            tlm.EXPTelemetry_mini.Telemetry_bin_gain_adc = (uint8_t)src[offset + 41];
            tlm.EXPTelemetry_mini.Telemetry_bin_gain_ui = (uint8_t)src[offset + 42];
            tlm.EXPTelemetry_mini.Telemetry_bin_status = (uint8_t)src[offset + 43];
           
            return tlm;
        }

        public static string SATStruct_str(SATTelemetry_t data)
        {
            string str = "";

            SATTelemetry_t tlm = data;

            str += tlm.COMTelemetry.RSSI_level.ToString() + ",";
            str += tlm.COMTelemetry.COM_MCU_temp.ToString() + ",";
            str += tlm.COMTelemetry.ADF_temp.ToString() + ",";

            str += tlm.CTRLTelemetry.MET.ToString() + ",";
            str += tlm.CTRLTelemetry.EXPStat.ToString() + ",";
            str += tlm.CTRLTelemetry.CTRL_MCU_temp.ToString() + ",";

            str += tlm.PWRTelemetry.MPP1.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP1.I.ToString() + ",";

            str += tlm.PWRTelemetry.MPP2.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP2.I.ToString() + ",";

            str += tlm.PWRTelemetry.MPP3.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP3.I.ToString() + ",";

            str += tlm.PWRTelemetry.SolarPanelTemperature.T_XT.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_XB.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_YT.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_YB.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_ZT.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_ZB.ToString() + ",";

            str += tlm.PWRTelemetry.BatteryTelemetry.U.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.I_Ch.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.I_Dis.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.T.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.Pwr_balance.ToString() + ",";

            str += tlm.PWRTelemetry.OtherTelemetry.VBUS_I.ToString() + ",";
            str += tlm.PWRTelemetry.OtherTelemetry.MCU_T.ToString() + ",";

            str += tlm.PWRTelemetry.PWRState.state.ToString() + ",";

             str += tlm.EXPbinTelemetry.Telemetry_bin_Hv_current.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_tok_magnitude.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_Hv_current.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_ion_tok_magnitude.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_ion_tok_exponent.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_adc.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_gain_adc.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_gain_ui.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_Temp.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_Hi_volt.ToString() + ",";
             str += tlm.EXPbinTelemetry.Telemetry_bin_status.ToString() + ",";

            return str;
        }

        public static string SATminiStruct_str(SATTelemetry_mini_t data)
        {
            string str = "";

            SATTelemetry_mini_t tlm = data;

           
            str += tlm.CTRLTelemetry.MET.ToString() + ",";
            str += tlm.CTRLTelemetry.EXPStat.ToString() + ",";
            str += tlm.CTRLTelemetry.CTRL_MCU_temp.ToString() + ",";

            str += tlm.PWRTelemetry.MPP1.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP1.I.ToString() + ",";

            str += tlm.PWRTelemetry.MPP2.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP2.I.ToString() + ",";

            str += tlm.PWRTelemetry.MPP3.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP3.I.ToString() + ",";

            str += tlm.PWRTelemetry.SolarPanelTemperature.T_XT.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_XB.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_YT.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_YB.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_ZT.ToString() + ",";
            str += tlm.PWRTelemetry.SolarPanelTemperature.T_ZB.ToString() + ",";

            str += tlm.PWRTelemetry.BatteryTelemetry.U.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.I_Ch.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.I_Dis.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.T.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.Pwr_balance.ToString() + ",";

            str += tlm.PWRTelemetry.OtherTelemetry.VBUS_I.ToString() + ",";
            str += tlm.PWRTelemetry.OtherTelemetry.MCU_T.ToString() + ",";

            str += tlm.PWRTelemetry.PWRState.state.ToString() + ",";

            str += tlm.EXPTelemetry_mini.Telemetry_bin_adc.ToString() + ",";
            str += tlm.EXPTelemetry_mini.Telemetry_bin_gain_adc.ToString() + ",";
            str += tlm.EXPTelemetry_mini.Telemetry_bin_gain_ui.ToString() + ",";
            str += tlm.EXPTelemetry_mini.Telemetry_bin_status.ToString() + ",";

            return str;
        }

        public static PWRTelemetry_packet_t fill_PWRStruct(byte[] src, int offset)
        {
            PWRTelemetry_packet_t tlm = new PWRTelemetry_packet_t();
            tlm.header = (uint16_t)((uint16_t)(src[offset + 1] * (uint16_t)256) + (uint16_t)src[offset + 0]);
            tlm.size = (uint8_t)src[offset + 2];
            tlm.PWRTelemetry.MPP1.U = (uint16_t)((uint16_t)(src[offset + 4] * (uint16_t)256) + (uint16_t)src[offset + 3]);
            tlm.PWRTelemetry.MPP1.I = (uint16_t)((uint16_t)(src[offset + 6] * (uint16_t)256) + (uint16_t)src[offset + 5]);
           // tlm.PWRTelemetry.MPP1.T = (int8_t)src[offset + 7];

            tlm.PWRTelemetry.MPP2.U = (uint16_t)((uint16_t)(src[offset + 9] * (uint16_t)256) + (uint16_t)src[offset + 6]);
            tlm.PWRTelemetry.MPP2.I = (uint16_t)((uint16_t)(src[offset + 11] * (uint16_t)256) + (uint16_t)src[offset + 10]);
            //tlm.PWRTelemetry.MPP2.T = (int8_t)src[offset + 12];

            tlm.PWRTelemetry.MPP3.U = (uint16_t)((uint16_t)(src[offset + 14] * (uint16_t)256) + (uint16_t)src[offset + 13]);
            tlm.PWRTelemetry.MPP3.I = (uint16_t)((uint16_t)(src[offset + 16] * (uint16_t)256) + (uint16_t)src[offset + 15]);
           // tlm.PWRTelemetry.MPP3.T = (int8_t)src[offset + 17];

            tlm.PWRTelemetry.BatteryTelemetry.U = (uint16_t)((uint16_t)(src[offset + 19] * (uint16_t)256) + (uint16_t)src[offset + 18]);
            tlm.PWRTelemetry.BatteryTelemetry.I_Ch = (uint16_t)((uint16_t)(src[offset + 21] * (uint16_t)256) + (uint16_t)src[offset + 20]);
            tlm.PWRTelemetry.BatteryTelemetry.I_Dis = (uint16_t)((uint16_t)(src[offset + 23] * (uint16_t)256) + (uint16_t)src[offset + 22]);
            tlm.PWRTelemetry.BatteryTelemetry.T = (int8_t)src[offset + 24];
            tlm.PWRTelemetry.BatteryTelemetry.Pwr_balance = (src[offset + 28] << 24) | (src[offset + 27] << 16) | (src[offset + 26] << 8) | (src[offset + 25] << 0);

            tlm.PWRTelemetry.OtherTelemetry.VBUS_I = (uint16_t)((uint16_t)(src[offset + 30] * (uint16_t)256) + (uint16_t)src[offset + 29]);
            tlm.PWRTelemetry.OtherTelemetry.MCU_T = (int8_t)src[offset + 31];

            tlm.PWRTelemetry.PWRState.state = (uint8_t)src[offset + 32];

            tlm.crc8 = (uint8_t)src[offset + 33];

            return tlm;
        }

        public static string PWRStruct_str (PWRTelemetry_packet_t data)
        {
            string str = "";
            PWRTelemetry_packet_t tlm = data;
            str += tlm.PWRTelemetry.MPP1.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP1.I.ToString() + ",";
            //str += tlm.PWRTelemetry.MPP1.T.ToString() + ",";
            
            str += tlm.PWRTelemetry.MPP2.U.ToString() + ",";
            str += tlm.PWRTelemetry.MPP2.I.ToString() + ",";
            //str += tlm.PWRTelemetry.MPP2.T.ToString() + ",";

            str += tlm.PWRTelemetry.MPP3.U.ToString() + ","; 
            str += tlm.PWRTelemetry.MPP3.I.ToString() + ","; 
           // str += tlm.PWRTelemetry.MPP3.T.ToString() + ","; 

            str += tlm.PWRTelemetry.BatteryTelemetry.U.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.I_Ch.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.I_Dis.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.T.ToString() + ",";
            str += tlm.PWRTelemetry.BatteryTelemetry.Pwr_balance.ToString() + ",";

            str += tlm.PWRTelemetry.OtherTelemetry.VBUS_I.ToString() + ",";
            str += tlm.PWRTelemetry.OtherTelemetry.MCU_T.ToString() + ",";

            return str;
        }
    }
}
