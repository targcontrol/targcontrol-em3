using FTD2XX_NET;
using System;
using System.Text;
using System.Threading;

namespace TargControl_EM3_Example
{
    class Program
    {
        static void Main(string[] args)
        {

            UInt32 ftdiDeviceCount = 0;
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

            var MFtdiDevice = new FTDI();

            ftStatus = MFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                throw new Exception("Error create device (error " + ftStatus + ")");
            }

            if (ftdiDeviceCount == 0)
            {
                throw new Exception("Error device count is 0");
            }

            ftStatus = MFtdiDevice.OpenByIndex(0);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                throw new Exception("Error open device by serial number (error " + ftStatus + ")");
            }

            // Set up device data parameters
            // Set Baud rate to 9600
            ftStatus = MFtdiDevice.SetBaudRate(9600);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                throw new Exception("Failed to set Baud rate (error " + ftStatus + ")");
            }

            // Set data characteristics - Data bits, Stop bits, Parity
            ftStatus = MFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1,
                                                           FTDI.FT_PARITY.FT_PARITY_NONE);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                throw new Exception("Failed to set data characteristics (error " + ftStatus + ")");
            }

            // Set flow control - set RTS/CTS flow control
            ftStatus = MFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                throw new Exception("Failed to set flow control (error " + ftStatus + ")");
            }

            // Set read timeout to 5 seconds, write timeout to infinite
            ftStatus = MFtdiDevice.SetTimeouts(5000, 0);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                throw new Exception("Failed to set timeouts (error " + ftStatus + ")");
            }

            Boolean continueToScan = true;

            while (continueToScan)
            {
                Console.WriteLine("Tap card to read it");

                UInt32 numBytesAvailable = 0;

                do
                {
                    ftStatus = MFtdiDevice.GetRxBytesAvailable(ref numBytesAvailable);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    {
                        throw new Exception("Failed to get number of bytes available to read (error " + ftStatus + ")");
                    }
                    Thread.Sleep(200);
                } while (numBytesAvailable < 1);


                var readData = new Byte[numBytesAvailable];
                var numBytesRead = 0U;

                ftStatus = MFtdiDevice.Read(readData, numBytesAvailable, ref numBytesRead);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    throw new Exception("Failed to read data (error " + ftStatus + ")");
                }

                Console.WriteLine("Received: " + numBytesRead + " bytes");

                //check control sum
                byte bcc = 0;
                for (int i = 1; i <= 7; i++)
                {
                    bcc ^= readData[i];
                }

                if (bcc != readData[8])
                {
                    throw new Exception("Checksum is not valid");
                }


                //EM-Marine code readData[5], readData[6], readData[7]
                var emMarineCode = new Byte[3];
                Array.Copy(readData, 5, emMarineCode, 0, 3);

                Console.WriteLine(GetEmMarineCodeString(emMarineCode));


                Console.WriteLine("Do you want to continue ? Y - yes, N - no");
                String line = Console.ReadLine();

                continueToScan = line == "Y";

            }

        }

        static string GetEmMarineCodeString(Byte[] code)
        {
            var first_code_int_int = (int)code[0];
            var first_code_tmp = first_code_int_int.ToString();

            var second_code_in_int = (code[1] << 8) | code[2];
            var second_code_tmp = second_code_in_int.ToString();

            StringBuilder sb = new StringBuilder();
            if (second_code_tmp.Length < 5)
            {
                sb.Append('0', 5 - second_code_tmp.Length);
            }
            sb.Append(second_code_tmp);
            var second_code = sb.ToString();

            sb.Clear();
            if (first_code_tmp.Length < 3)
            {
                sb.Append('0', 3 - first_code_tmp.Length);
            }
            sb.Append(first_code_tmp);
            var first_code = sb.ToString();

            return $"{first_code},{second_code}";
        }
    }
}
