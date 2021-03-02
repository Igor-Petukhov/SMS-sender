using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMS_sender
{
   public partial class Program
    {
        // Display Port values and prompt user to enter a port.
        public static string SetPortName(string defaultPortName)
        {
            string portName;

            Console.WriteLine("Доступные порты:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Введите COM значение порта (напимер так: COM1) (По умолчанию: {0}): ", defaultPortName);
            portName = Console.ReadLine();

            if (portName == "" || !(portName.ToLower()).StartsWith("com"))
            {
                portName = defaultPortName;
            }
            return portName;
        }
        // Display BaudRate values and prompt user to enter a value.
        public static int SetPortBaudRate(int defaultPortBaudRate)
        {
            string baudRate;

            Console.Write("Baud Rate(default:{0}): ", defaultPortBaudRate);
            baudRate = Console.ReadLine();

            if (baudRate == "")
            {
                baudRate = defaultPortBaudRate.ToString();
            }

            return int.Parse(baudRate);
        }

        // Display PortParity values and prompt user to enter a value.
        public static Parity SetPortParity(Parity defaultPortParity)
        {
            string parity;

            Console.WriteLine("Available Parity options:");
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Parity value (Default: {0}):", defaultPortParity.ToString(), true);
            parity = Console.ReadLine();

            if (parity == "")
            {
                parity = defaultPortParity.ToString();
            }

            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }
        // Display DataBits values and prompt user to enter a value.
        public static int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits;

            Console.Write("Enter DataBits value (Default: {0}): ", defaultPortDataBits);
            dataBits = Console.ReadLine();

            if (dataBits == "")
            {
                dataBits = defaultPortDataBits.ToString();
            }

            return int.Parse(dataBits.ToUpperInvariant());
        }

        // Display StopBits values and prompt user to enter a value.
        public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits;

            Console.WriteLine("Available StopBits options:");
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter StopBits value (None is not supported and \n" +
             "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
            stopBits = Console.ReadLine();

            if (stopBits == "")
            {
                stopBits = defaultPortStopBits.ToString();
            }

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }
        public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake;

            Console.WriteLine("Available Handshake options:");
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Handshake value (Default: {0}):", defaultPortHandshake.ToString());
            handshake = Console.ReadLine();

            if (handshake == "")
            {
                handshake = defaultPortHandshake.ToString();
            }

            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }


        /// <summary>
        /// Отправка/приём данных через SerialPort.
        /// </summary>
        /// <param name="connection">SerialPort, используемый для связи.</param>
        /// <param name="sendBuffer">Массив отправляемых байт.</param>
        /// <param name="waitingForStart">Время ожидания от отправки до начала ответа.</param>
        /// <param name="waitForEnd">Время ожидания после завершения ответа.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <param name="recvBuffer">Массив буфера для приёма байт.</param>
        /// <param name="received">Количество принятых байт.</param>
        /// <returns>Получен ли какой-нибудь ответ.</returns>
        public static bool SendRecv(SerialPort connection, byte[] sendBuffer, int waitingForStart, int waitForEnd, CancellationToken cancellationToken, ref byte[] recvBuffer, out int received)
        {
            bool result = false;
            received = -1;

            //Очистим буфер ответа сканера, что бы не накладывались ответы
            Array.Clear(recvBuffer, 0, recvBuffer.Length);

            try
            {
                // Порт должен быть открыт.
                if (connection.IsOpen)
                {
                    // Сброс буферов.
                    connection.DiscardInBuffer();
                    connection.DiscardOutBuffer();

                    if (connection.BytesToRead > 0)
                    {
                        connection.ReadExisting();
                    }

                    // Установка таймаута начала ответа.
                    connection.ReadTimeout = waitingForStart;

                    // Отправка данных.
                    connection.Write(sendBuffer, 0, sendBuffer.Length);

                    int readed = 0;

                    try
                    {
                        // Ожидание первого байта. Если ничего не придёт за waitingForStart мс, то вылетит TimeoutException.
                        recvBuffer[readed++] = Convert.ToByte(connection.ReadByte());

                        // Установка таймаута для определения завершения ответа.
                        connection.ReadTimeout = waitForEnd;

                        try
                        {
                            // Временный буфер для приёма частями.
                            byte[] temp = new byte[16];

                            do
                            {
                                // Чтение и заполнение буфера.
                                // Если ничего не принято, то вылетит TimeoutException.
                                // Если считано меньше или равно размеру буфера, это число возвращается функцией.
                                int b = connection.Read(temp, 0, 16);

                                // Копирование в выходной буфер.
                                for (int i = 0; i < b; i++)
                                {
                                    recvBuffer[readed++] = temp[i];
                                }

                                // Повтор до явной отмены.
                                // Также, выход доступен по таймауту чтения.
                            } while (!cancellationToken.IsCancellationRequested);
                        }
                        catch (TimeoutException)
                        {
                        }
                        finally
                        {
                            // Количество считанного.
                            received = readed;
                            // Возвращаемый результат: принято ли что-нибудь.
                            result = readed > 0;
                        }
                    }
                    catch (TimeoutException)
                    {
                    }
                }
                else //если порт закрыт
                {
                    Console.WriteLine("Соединение не установлено!");
                }
            }
            catch (Exception e)
            {
                result = false;
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Ответ:\n" + Encoding.ASCII.GetString(recvBuffer));
            return result;
        }

        public static void Send_to_port(string command)
        {
            send_data = Encoding.ASCII.GetBytes(command);
            //New fast method
            SendRecv(_serialPort, send_data, waitingForStart, waitForEnd, token, ref receive_data, out received_bytes);

            ////My simple method
            //send_data = Encoding.ASCII.GetBytes(command); //Перевести строку в байты
            //try
            //{
            //    _serialPort.Write(send_data, 0, send_data.Length); //Отправить в открытый порт
            //}
            //catch (Exception ex_open_comport)
            //{
            //    MessageBox.Show(ex_open_comport.Message);
            //}
            Console.WriteLine("...receiving stopped...");
        }
    }
}
