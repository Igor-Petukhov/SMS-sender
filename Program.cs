using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace SMS_sender
{
    public partial class Program
    {
        static void ShowMenu()
        {
            Console.WriteLine("777 - show menu");
            Console.WriteLine("0 - exit");
            Console.WriteLine("101 - AT+CFUN=1 - reboot modem");
            Console.WriteLine("1 - ATI info");
            Console.WriteLine("2 - ATD+380934188301;"); //позвонить абоненту
            Console.WriteLine("3 - AT+CSQ - посмотреть уровень радиосигнала");
            Console.WriteLine("4 - AT+CLAC в ответе будет список поддерживаемых команд");
            Console.WriteLine("=========================================================");
            Console.WriteLine("5 - AT+CMGF=1 - установка текстового режима");
            Console.WriteLine("51 - AT+CMGF=0 - установка режима PDU");
            Console.WriteLine("6 - AT+CSMP=17,167,0,0 - установка параметров текстового режима");
            Console.WriteLine("7 - AT+CMGS=\"+380934188301\" - номер получателя SMS. > - ответ модуля(модуль готов принять текст SMS)");
            Console.WriteLine("8 - test one 0x1A===TEXT - ввод и отправка текста в модуль. Как только в тексте встретится символ <0x1A>, сообщение будет отправлено. Если в тексте встретится символ <0x1B>, сообщение не будет отправлено");
            Console.WriteLine("9 - AT+CNMA - запрос отчета о доставке");
            Console.WriteLine("=========================================================");
            Console.WriteLine("10 - ATD+380934188301 30 минут");
            Console.WriteLine("11 - AT+CNUM - Запрос номера абонента MSISDN (вывести свой номер телефона).");
            Console.WriteLine("12 - AT+CHUP положить трубку");
            Console.WriteLine("=========================================================");
            Console.WriteLine("13 - AT+CPMS=\"MT\" - настройка памяти");
            Console.WriteLine("131 - AT+CPMS=\"MT\",\"MT\" - настройка памяти");
            Console.WriteLine("132 - AT+CPMS=\"MT\",\"MT\",\"MT\" - настройка памяти");
            Console.WriteLine("14 - AT+CMGL=4 - PDU mode чтение всех SMS из памяти");
            Console.WriteLine("141 - AT+CMGL=\"ALL\" - text mode чтение всех SMS из памяти");
            Console.WriteLine("142 - AT+CMGL=0 - PDU mode чтение new SMS из памяти");
            Console.WriteLine("1421 - AT+CMGL=\"REC UNREAD\" - text mode чтение new SMS из памяти");
            Console.WriteLine("15 - AT+CMGR=0 - чтение самого первого SMS (с индексом 0) из памяти");
            Console.WriteLine("16 - AT+CPMS=? - текущая настройка памяти модема для смс");
            Console.WriteLine("17 - AT+CUSD=1,AA582C3602,15 - USSD balance *111#");
            Console.WriteLine("18 - AT+CSCS=? - поддерживаемые кодировки");
            Console.WriteLine("19 - AT+CSCS=\"IRA\" - use кодировкa IRA");
            Console.WriteLine("192 - считать и декодировать ответ баланса");
            Console.WriteLine("20 - СЧИТАТЬ ОТВЕТ ПОРТА");
        }

        //для работы с ком портом
        public static SerialPort _serialPort = new SerialPort();
        static byte[] send_data = new byte[1500];
        static byte[] receive_data = new byte[1500];
        public static int received_bytes;
        public static int waitingForStart = 500;
        public static int waitForEnd = 500;

        //Самая важная и непонятная штука для декодирования USSD ответа
        public static byte[] _decodeMask = { 128, 192, 224, 240, 248, 252, 254 };


        public static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        public static CancellationToken token;
        static void Main(string[] args)
        {
            

            //Если существует конфигурационный файл - считаем настройки из него
            if (File.Exists("settings.txt"))
            {
                using (FileStream fs = new FileStream("settings.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.Unicode))
                    {
                        _serialPort.PortName = sr.ReadLine(); //Имя порта
                        _serialPort.BaudRate = Int32.Parse(sr.ReadLine());
                        _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), sr.ReadLine(), true);
                        _serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), sr.ReadLine(), true);
                        _serialPort.DataBits = Int32.Parse(sr.ReadLine());
                        _serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), sr.ReadLine(), true);

                    }
                }
            }
            else
            {
                Console.WriteLine("Для соединения со 3G модемом необходимо ввести настройки подключения. Создастся файл настроек settings.txt.");
                // Allow the user to set the appropriate properties.
                Console.WriteLine("Стандартные настройки соединения:\n\tPortName = COM6\n\tBaudRate = 9600\n\tParity = None\n\tStopBits = One\n\tDataBits = 8\n\tHandshake = None");
                // Пользователь введёт настройки и мы их сохраним в файл

                using (FileStream fs = new FileStream("settings.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode))
                    {
                        _serialPort.PortName = SetPortName(_serialPort.PortName);
                        sw.WriteLine(_serialPort.PortName);

                        _serialPort.BaudRate = SetPortBaudRate(_serialPort.BaudRate);
                        sw.WriteLine(_serialPort.BaudRate);

                        _serialPort.Parity = SetPortParity(_serialPort.Parity);
                        sw.WriteLine(_serialPort.Parity);

                        _serialPort.StopBits = SetPortStopBits(_serialPort.StopBits);
                        sw.WriteLine(_serialPort.StopBits);

                        _serialPort.DataBits = SetPortDataBits(_serialPort.DataBits);
                        sw.WriteLine(_serialPort.DataBits);

                        _serialPort.Handshake = SetPortHandshake(_serialPort.Handshake);
                        sw.WriteLine(_serialPort.Handshake);
                    }
                }
                Console.WriteLine("Настройки сохранены в файл settings.txt\nДля продолжения введите любой символ.");
                Console.ReadKey();
                Console.Clear();
            }

            _serialPort.Open();

            ShowMenu();
            int choise;
            
            do
            {
                choise = Int32.Parse(Console.ReadLine());
                switch (choise)
                {
                    case 777:
                        ShowMenu();
                        break;
                    case 101://101 - AT+CFUN=1 - reboot modem
                        Send_to_port("AT+CFUN=1\r\n");
                        break;
                    case 1:
                        Send_to_port("ATI\r\n");
                        break;
                    case 2:
                        Send_to_port("ATD+380934188301;\r\n");
                        break;
                    case 3:
                        Send_to_port("AT+CSQ\r\n");
                        break;
                    case 4:
                        Send_to_port("AT+CLAC\r\n");
                        break;

                    case 5:
                        Send_to_port("AT+CMGF=1\r\n");
                        break;
                    case 51:
                        Send_to_port("AT+CMGF=0\r\n");
                        break;
                    case 6:
                        Send_to_port("AT+CSMP=17,167,0,0\r\n");
                        break;
                    case 7:
                        Send_to_port("AT+CMGS=\"+380934188301\"\r\n");
                        break;
                    case 8:
                        Send_to_port("test one " + Convert.ToChar(0x1A) + "\r\n");
                        break;
                    case 9:
                        Send_to_port("AT+CNMA\r\n");
                        break;
                    case 10: //10 - ATD+380934188301 30 минут
                        TimeSpan interval = new TimeSpan(0, 30, 0);
                        DateTime time_to_stop = DateTime.Now + interval;
                        while (DateTime.Now < time_to_stop)
                        {
                            Console.WriteLine(DateTime.Now + "<" + time_to_stop);
                            Send_to_port("ATD+380934188301;\r\n");
                            Thread.Sleep(new TimeSpan(0, 1, 0));
                        }
                        break;
                    case 11://11 - AT+CNUM - Запрос номера абонента MSISDN (вывести свой номер телефона).
                        Send_to_port("AT+CNUM\r\n");
                        break;
                    case 12: //12 - AT+CHUP положить трубку
                        Send_to_port("AT+CHUP\r\n");
                        break;
                    case 13://13 - AT+CPMS=\"MT\" - настройка памяти");
                        Send_to_port("AT+CPMS=\"MT\"\r\n");
                        break;
                    case 131://Console.WriteLine("131 - AT+CPMS=\"MT\",\"MT\" - настройка памяти");
                        Send_to_port("AT+CPMS=\"MT\",\"MT\"\r\n");
                        break;
                    case 132:// Console.WriteLine("132 - AT+CPMS=\"MT\",\"MT\",\"MT\" - настройка памяти");
                        Send_to_port("AT+CPMS=\"MT\",\"MT\",\"MT\"\r\n");
                        break;
                    case 14://Console.WriteLine("14 - AT+CMGL=4 - чтение всех SMS из памяти");
                        Send_to_port("AT+CMGL=4\r\n");
                        break;
                    case 141://141 - AT+CMGL=ALL - PDU mode чтение всех SMS из памяти
                        Send_to_port("AT+CMGL=\"ALL\"\r\n");
                        break;
                    case 15://Console.WriteLine("15 - AT+CMGR=0 - чтение самого первого SMS (с индексом 0) из памяти");
                        Send_to_port("AT+CMGR=0\r\n");
                        break;
                    case 142://Console.WriteLine("142 - AT+CMGL=0 - PDU mode чтение new SMS из памяти");
                        Send_to_port("AT+CMGL=0\r\n");
                        break;
                    case 1421://Console.WriteLine("1421 - AT+CMGL=\"REC UNREAD\" - text mode чтение new SMS из памяти");
                        Send_to_port("AT+CMGL=\"REC UNREAD\"\r\n");
                        break;
                    case 16://16 - AT+CPMS=? - текущая настройка памяти модема для смс
                        Send_to_port("AT+CPMS=?\r\n");
                        break;
                    case 17://17 - AT+CUSD=1,*111#,15 - USSD balance
                        Send_to_port("AT+CUSD=1,AA582C3602,15\r\n");
                        break;
                    case 18: //18 - AT+CSCS=? - поддерживаемые кодировки
                        Send_to_port("AT+CSCS=?\r\n");
                        break;
                    case 19://19 - AT+CSCS=\"IRA\" - use кодировкa IRA
                        Send_to_port("AT+CSCS=\"IRA\"\r\n");
                        break;
                    case 192: //192 - считать и декодировать ответ баланса
                        Array.Clear(receive_data, 0, receive_data.Length);
                        _serialPort.Read(receive_data, 0, receive_data.Length);
                        int ind_start = Encoding.ASCII.GetString(receive_data).IndexOf("+CUSD: 0,\"");
                        int ind_stop = Encoding.ASCII.GetString(receive_data).IndexOf("\",15");
                        int lenght = ind_stop - ind_start;
                        if (ind_start != -1)
                        {
                            string unswer = Encoding.ASCII.GetString(receive_data).Substring(ind_start + 10, lenght - 10);
                            Console.WriteLine(unswer);

                            byte[] packedBytes = ConvertHexToBytes(unswer);
                            byte[] unpackedBytes = UnpackBytes(packedBytes);

                            //gahi in kar mikone gahi balkaee nafahmidam chera
                            string o = Encoding.Default.GetString(unpackedBytes);
                            Console.WriteLine(o);
                        }
                        else
                            Console.WriteLine("No USSD unswer");
                        break;
                    case 20: //"20 - СЧИТАТЬ ОТВЕТ ПОРТА
                        Array.Clear(receive_data, 0, receive_data.Length);
                        _serialPort.Read(receive_data, 0, receive_data.Length);
                        Console.WriteLine("Ответ:\n" + Encoding.ASCII.GetString(receive_data));
                        break;
                    default:
                        break;


                }
            } while (choise != 0);


            Console.WriteLine("bye-bye...");
            Console.ReadKey();
            _serialPort.Close();
        }

        //convert USSD//////////////////////////////////////////////////////////////////////////
        public static byte[] ConvertHexToBytes(string hexString)
        {
            if (hexString.Length % 2 != 0)
                return null;

            int len = hexString.Length / 2;
            byte[] array = new byte[len];

            for (int i = 0; i < array.Length; i++)
            {
                string tmp = hexString.Substring(i * 2, 2);
                array[i] =
                byte.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
            }

            return array;
        }

        public static byte[] UnpackBytes(byte[] packedBytes)
        {
            byte[] shiftedBytes = new byte[(packedBytes.Length * 8) / 7];

            int shiftOffset = 0;
            int shiftIndex = 0;

            // Shift the packed bytes to the left according 
            //to the offset (position of the byte)
            foreach (byte b in packedBytes)
            {
                if (shiftOffset == 7)
                {
                    shiftedBytes[shiftIndex] = 0;
                    shiftOffset = 0;
                    shiftIndex++;
                }

                shiftedBytes[shiftIndex] = (byte)((b << shiftOffset) & 127);

                shiftOffset++;
                shiftIndex++;
            }

            int moveOffset = 0;
            int moveIndex = 0;
            int unpackIndex = 1;
            byte[] unpackedBytes = new byte[shiftedBytes.Length];

            // 
            if (shiftedBytes.Length > 0)
            {
                unpackedBytes[unpackIndex - 1] =
                shiftedBytes[unpackIndex - 1];
            }

            // Move the bits to the appropriate byte (unpack the bits)
            foreach (byte b in packedBytes)
            {
                if (unpackIndex != shiftedBytes.Length)
                {
                    if (moveOffset == 7)
                    {
                        moveOffset = 0;
                        unpackIndex++;
                        unpackedBytes[unpackIndex - 1] =
                        shiftedBytes[unpackIndex - 1];
                    }

                    if (unpackIndex != shiftedBytes.Length)
                    {
                        // Extract the bits to be moved
                        int extractedBitsByte = (packedBytes[moveIndex] &
                                                _decodeMask[moveOffset]);
                        // Shift the extracted bits to the proper offset
                        extractedBitsByte =
                                   (extractedBitsByte >> (7 - moveOffset));
                        // Move the bits to the appropriate byte 
                        //(unpack the bits)
                        int movedBitsByte =
                          (extractedBitsByte | shiftedBytes[unpackIndex]);

                        unpackedBytes[unpackIndex] = (byte)movedBitsByte;

                        moveOffset++;
                        unpackIndex++;
                        moveIndex++;
                    }
                }
            }

            // Remove the padding if exists
            if (unpackedBytes[unpackedBytes.Length - 1] == 0)
            {
                byte[] finalResultBytes = new byte[unpackedBytes.Length - 1];
                Array.Copy(unpackedBytes, 0,
                           finalResultBytes, 0, finalResultBytes.Length);
                return finalResultBytes;
            }
            return unpackedBytes;
        }



    }
}
