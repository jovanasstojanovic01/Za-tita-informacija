using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Numerics;
using System.Collections.Generic;

namespace Zastita
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        public StreamReader STR;
        public StreamWriter STW;
        private string recieve, TextToSend, fileContent, selectedFilePath;
        private readonly Config configData;
        private bool validKey, picked, pickedBifid, file;
        private readonly Hashtable table;
        private int c;
        private uint[] L;
        private readonly uint[] S;
        private DialogResult result;
        private readonly int[,] Sigma ={
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
            { 14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3 },
            { 11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4 },
            { 7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8 },
            { 9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13 },
            { 2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9 },
            { 12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11 },
            { 13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10 },
            { 6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5 },
            { 10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0 }
        };
        private readonly uint[] n = {
            0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A,
            0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19,
            0x662EAA7A, 0x8C1D5D95, 0xD5A79147, 0x983E5152,
            0x2E679B02, 0xA9D2C907, 0x5207D3C1, 0x76D22E98
        };
        public Form1()
        {
            InitializeComponent();
            configData = new Config();
            table = new Hashtable();
            validKey = false;
            picked = false;
            pickedBifid = false;
            S = new uint[2 * configData.R + 4];
            file = false;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            btnSend.Visible = false;
            string tmp = configData.Key;
            if (tmp.Length == 25)
            {
                for (int i = 1; i < 6; i++)
                {
                    for (int j = 1; j < 6; j++)
                    {
                        table.Add(tmp[(i - 1) * 5 + j - 1], (i * 10 + j).ToString());
                        table.Add((i * 10 + j).ToString(), tmp[(i - 1) * 5 + j - 1]);
                    }
                }
                validKey = true;
            }

            c = (configData.RC6Key.Length+7) / 8;
            L = new uint[c];
            string rc = configData.RC6Key;
            for(int i=0;i<c;i++)
            {
                string noviHexaBroj= "";
                if (i*8 + 8 > configData.RC6Key.Length)
                {
                    noviHexaBroj += rc.Substring(i*8);
                    int razlika = i*8 + 8 - configData.RC6Key.Length;
                    for (int j = 0; j < razlika; j++)
                        noviHexaBroj += "0";
                }
                else
                    noviHexaBroj += rc.Substring(i*8, 8);

                L[i] = ToUintFromHexFromString(noviHexaBroj);

            }
            GenerateKeys();

        }
        private void btnListen_Click(object sender, EventArgs e)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, configData.Port);
            listener.Start();
            Invoke(new Action(() =>
                {
                    btnConnect.Visible = false;
                    btnListen.Visible = false;
                    comboBox1.Visible = true;
                }));
            client = listener.AcceptTcpClient();
            Invoke(new Action(() => btnSend.Visible = true));
            STR = new StreamReader(client.GetStream());
            STW = new StreamWriter(client.GetStream());
            STW.AutoFlush = true;
            backgroundWorker1.RunWorkerAsync();
            backgroundWorker2.WorkerSupportsCancellation = true;
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (client.Connected)
            {
                
                try
                {
                    recieve = STR.ReadLine();
                    if (recieve.StartsWith("FILE:"))
                    {
                        string fileText = recieve.Substring(5);
                        int indeksPrvogBlanka = fileText.IndexOf(' ');
                        int heshLength = Convert.ToInt32(fileText.Substring(0,indeksPrvogBlanka));
                        string heshed = fileText.Substring(indeksPrvogBlanka + 1, heshLength);
                        fileText = fileText.Substring(indeksPrvogBlanka+1+ heshLength);
                        
                        string decryptedMsg = "";
                        if (picked)
                        {
                            decryptedMsg = helpRecieving(fileText);
                        }

                        if (CalculateBlake256(decryptedMsg) == heshed)
                        {
                            lbCryptedMessages.Invoke(new MethodInvoker(delegate ()
                            {
                                lbCryptedMessages.Items.Add("Sender: File recieved");
                            }));

                            lbMessages.Invoke(new MethodInvoker(delegate ()
                            {
                                lbMessages.Items.Add("Sender: File recieved");
                            }));

                            DialogResult result = MessageBox.Show($"Primljen je fajl\nŽelite li ga preuzeti u C:/Users/Public/Downloads?", "Primljen fajl", MessageBoxButtons.YesNo);

                            if (result == DialogResult.Yes)
                            {
                                string newName = "received.txt";
                                string receivedFilePath = Path.Combine("C:/Users/Public/Downloads", newName);
                                File.WriteAllText(receivedFilePath, decryptedMsg);
                            }
                        }
                        else
                        {
                            lbCryptedMessages.Invoke(new MethodInvoker(delegate ()
                            {
                                lbCryptedMessages.Items.Add("Sender: Error occurred");
                            }));

                            lbMessages.Invoke(new MethodInvoker(delegate ()
                            {
                                lbMessages.Items.Add("Sender: Error occurred");
                            }));
                        }
                        
                    }
                    else
                    {
                        lbCryptedMessages.Invoke(new MethodInvoker(delegate ()
                        {
                            lbCryptedMessages.Items.Add("Sender: " + recieve);
                        }));

                        if (picked)
                        {
                            string decryptedMsg = helpRecieving(recieve);

                            lbMessages.Invoke(new MethodInvoker(delegate ()
                            {
                                lbMessages.Items.Add("Sender: " + decryptedMsg);
                            }));
                        }
                        else
                        {
                            lbMessages.Invoke(new MethodInvoker(delegate ()
                            {
                                lbMessages.Items.Add("Sender: " + recieve);
                            }));
                        }

                        recieve = "";
                    }
                }
                catch
                {
                    MessageBox.Show("Connection closed");
                    Invoke(new Action(() => Close()));
                }
            }
        }
        private string helpRecieving(string recieved)
        {
            string decryptedMsg;
            if (pickedBifid)
            {
                if (validKey)
                {
                    decryptedMsg = BifidDecrypt(recieved, table);
                }
                else
                {
                    decryptedMsg = recieved;
                }
            }
            else
            {
                decryptedMsg = HexToString(OFBRC6(recieved));
            }
            return decryptedMsg;
        }
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            if (client.Connected)
            {
                lbMessages.Invoke(new MethodInvoker(delegate ()
                {
                    lbMessages.Items.Add("Me: " + TextToSend.ToLower());
                }));
                if (file)
                {

                    file = false;

                    if (File.Exists(selectedFilePath))
                    {

                        try
                        {
                            
                            fileContent = File.ReadAllText(selectedFilePath);
                            string hashed = ""; 
                            string cryptedTextToSend = "";
                            if (picked)
                            {
                                cryptedTextToSend = helpSending(fileContent.ToLower(), ref hashed, true);
                            }
                            
                            int hashLength = hashed.Length;
                            string tmpContent = "FILE:" + hashLength.ToString() + " "+ hashed + cryptedTextToSend;
                            
                            STW.WriteLine(tmpContent, 0, tmpContent.Length);

                            lbCryptedMessages.Invoke(new MethodInvoker(delegate ()
                            {
                                lbCryptedMessages.Items.Add("Me: File sent");
                            }));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error sending file: " + ex.Message);
                        }
                    }
                }
                else
                if (picked)
                {
                    string cryptedTextToSend = "";
                    string dummy = "";
                    cryptedTextToSend = helpSending(TextToSend.ToLower(), ref dummy, false);

                    TextToSend = "";
                    
                    STW.WriteLine(cryptedTextToSend);
                    lbCryptedMessages.Invoke(new MethodInvoker(delegate ()
                    {
                        lbCryptedMessages.Items.Add("Me: " + cryptedTextToSend);
                    }));
                   
                }
                else
                {
                    STW.WriteLine(TextToSend);
                    lbCryptedMessages.Invoke(new MethodInvoker(delegate ()
                    {
                        lbCryptedMessages.Items.Add("Me: "+ TextToSend.ToLower());
                    }));
                }
                
            }
            else
            {
                MessageBox.Show("Sending Failed");
            }
            backgroundWorker2.CancelAsync();
        }
        private string helpSending(string text, ref string hash, bool fileHash)
        {
            string crypted;
            if (pickedBifid)
            {
                if (validKey)
                {
                    crypted = Bifid(text, table);
                    if(fileHash)
                        hash = CalculateBlake256(BifidDecrypt(crypted, table));
                }
                else
                {
                    crypted = text;
                    if (fileHash)
                        hash = CalculateBlake256(BifidDecrypt(text, table));
                }
            }
            else
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(text);
                string hexString = BitConverter.ToString(byteArray).Replace("-", "");
                crypted = OFBRC6(hexString);
                if (fileHash)
                    hash = CalculateBlake256(HexToString(OFBRC6(crypted)));
            }
            return crypted;
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            client = new TcpClient();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(configData.ServerAddress), configData.Port);
            
            try
            {
                client.Connect(endPoint);
                Invoke(new Action(() =>
                {
                    btnListen.Visible = false;
                    btnConnect.Visible = false;
                    comboBox1.Visible = true;
                }));
                Invoke(new Action(() => btnSend.Visible = true));
                STW = new StreamWriter(client.GetStream());
                STR = new StreamReader(client.GetStream());
                STW.AutoFlush = true;
                backgroundWorker1.RunWorkerAsync();
                backgroundWorker2.WorkerSupportsCancellation = true;
            }
            catch
            {
                MessageBox.Show("Chosen PORT is not Listening");
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (tbMessage.Text!="")
            {
                TextToSend = tbMessage.Text;
                backgroundWorker2.RunWorkerAsync();
            }
            tbMessage.Text = "";
        }

        // BLAKE i fajlovi /////////////////////////////////////////////////
        private string CalculateBlake256(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] inputBytesNew = PrepareMessage(inputBytes);
            uint[] message = new uint[inputBytesNew.Length / 4];

            for (int i = 0; i < inputBytesNew.Length; i += 4)
            {
                message[i / 4] = BitConverter.ToUInt32(inputBytesNew, i);
            }

            uint[] result = Blake256(message, 14);

            StringBuilder hashBuilder = new StringBuilder();

            foreach (uint value in result)
            {
                hashBuilder.Append(value.ToString("x16"));
            }

            return hashBuilder.ToString();
        }
        private static byte[] PrepareMessage(byte[] input)
        {
            List<byte> preparedMessage = new List<byte>(input);
            preparedMessage.Add(0x80);  // bajt 0x80 (binarno: 10000000)

            while ((preparedMessage.Count % 64) != 56)
            {
                preparedMessage.Add(0x00);  // bajt 0x00 (binarno: 00000000)
            }

            //little-endian
            ulong messageLength = (ulong)input.Length * 8;
            byte[] lengthBytes = BitConverter.GetBytes(messageLength);
            preparedMessage.AddRange(lengthBytes);

            return preparedMessage.ToArray();
        }
        private uint[] Blake256(uint[] m, int rounds)
        {
            uint[] h = new uint[n.Length];
            n.CopyTo(h, 0);

            uint a = 0x6A09E667;
            uint b = 0xBB67AE85;
            uint c = 0x3C6EF372;
            uint d = 0xA54FF53A;

            int g = m.Length;//t

            for (int blockIndex = 0; blockIndex < g; blockIndex += 16)
            {
                uint[] block = new uint[16];
                Array.Copy(m, blockIndex, block, 0, 16);

                for (int round = 0; round < rounds; ++round)
                {
                    for (int i = 0; i < 8; ++i)
                    {
                        int j = Sigma[round % 10, 2 * i];
                        int k = Sigma[round % 10, 2 * i + 1];

                        G(ref a, ref b, ref c, ref d, block[j] ^ n[k], block[k] ^ n[j]);

                        h[i] = h[i] + a + m[j];
                        h[i] = RotateRight((h[i] ^ h[i + 8]), 16) | RotateLeft((h[i] ^ h[i + 8]), (32 - 16));
                        h[i + 8] = h[i + 8] + h[i];
                        h[i + 8] = RotateRight((h[i + 8] ^ h[i]), 12) | RotateLeft((h[i + 8] ^ h[i]), (32 - 12));
                    }
                }
            }

            return h;
        }
        private void G(ref uint a, ref uint b, ref uint c, ref uint d, uint x, uint y)
        {
            a = (a + b) & 0xFFFFFFFF;
            a = (a + x) & 0xFFFFFFFF;
            d = d ^ a;
            d = RotateRight(d, 16);
            c = (c + d) & 0xFFFFFFFF;
            b = b ^ c;
            b = RotateRight(b, 12);
            a = (a + b) & 0xFFFFFFFF;
            a = (a + y) & 0xFFFFFFFF;
            d = d ^ a;
            d = RotateRight(d, 8);
            c = (c + d) & 0xFFFFFFFF;
            b = b ^ c;
            b = RotateRight(b, 7);
        }
        private void btnSendFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Tekstualni fajlovi (*.txt)|*.txt";
            result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                selectedFilePath = openFileDialog1.FileName;
                TextToSend = selectedFilePath;
                file = true;
                backgroundWorker2.RunWorkerAsync();
            }
        }

        /////////////////////////////////////////////////////////////////// 

        // RC6 sa OFB /////////////////////////////////////////////////////
        private void GenerateKeys()
        {
            S[0] = configData.P;
            for (int k = 1; k < 2 * configData.R + 4; k++)
            {
                S[k] = S[k - 1] + configData.Q;
            }

            uint A, B, i, j;
            A = B = i = j = 0;

            int v = 3 * Math.Max(c, 2 * configData.R + 4);

            for (int s = 1; s <= v; s++)
            {
                A = S[i] = RotateLeft((S[i] + A + B), 3);
                B = L[j] = RotateLeft((L[j] + A + B), (int)(A + B));
                i = (uint)((i + 1) % (2 * configData.R + 4));
                j = (uint)((j + 1) % c);
            }
        }
        private string OFBRC6(string tekst)
        {
            string kriptovano = "";

            int k = configData.IV.Length;
            int brojBlokova = (tekst.Length + k - 1) / k;

            string niv = configData.IV;
            for (int i = 0; i < brojBlokova; i++)
            {
                niv = RC6(niv);

                BigInteger intVal1;

                if (i == brojBlokova - 1)
                {
                    string hexstring1 = tekst.Substring(i * k);
                    hexstring1 = hexstring1.PadLeft(k, '0');

                    intVal1 = BigInteger.Parse(hexstring1, System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    intVal1 = BigInteger.Parse(tekst.Substring(i * k, k), System.Globalization.NumberStyles.HexNumber);
                }

                BigInteger intVal2 = BigInteger.Parse(niv, System.Globalization.NumberStyles.HexNumber);
                BigInteger result = intVal1 ^ intVal2;

                string resultStr = result.ToString("X");
                kriptovano += resultStr;
            }

            return kriptovano;
        }
        private string RC6(string tekst)
        {
            string kriptovano = "";

            uint A = ToUintFromHexFromString(tekst.Substring(0, 8));
            uint B = ToUintFromHexFromString(tekst.Substring(8, 8));
            uint C = ToUintFromHexFromString(tekst.Substring(16, 8));
            uint D = ToUintFromHexFromString(tekst.Substring(24, 8));

            B = B + S[0];
            D = D + S[1];
            for (int i = 1; i <= configData.R; i++)
            {
                uint t = RotateLeft((B * (2 * B + 1)), (int)(Math.Log10((double)configData.W) / Math.Log10(2)));
                uint u = RotateLeft((D * (2 * D + 1)), (int)(Math.Log10((double)configData.W) / Math.Log10(2)));
                A = RotateLeft((A ^ t), (int)u) + S[2 * i];
                C = RotateLeft((C ^ u), (int)t) + S[2 * i + 1];

                rotiraj(ref A, ref B, ref C, ref D);
            }

            A = A + S[2 * configData.R + 2];
            C = C + S[2 * configData.R + 3];

            A = ToUintFromHexFromString(A.ToString("X"));
            B = ToUintFromHexFromString(B.ToString("X"));
            C = ToUintFromHexFromString(C.ToString("X"));
            D = ToUintFromHexFromString(D.ToString("X"));

            kriptovano += A.ToString("X") + ""
                + B.ToString("X") + ""
                + C.ToString("X") + ""
                + D.ToString("X");

            return kriptovano;
        }
        
        /////////////////////////////////////////////////////////////////// 

        // Bifid ////////////////////////////////////////////////////////////
        private string Bifid(string input, Hashtable table)
        {
            string output = "";
            int n = input.Length;
            int[] cryptedCoordinats = new int[2 * n];
            int[] firstCoordinates = new int[n];
            int[] secondCoordinates = new int[n];
            int cordinatesCounter = 0;
            int cryptedCounter = 0;

            for (int i = 0; i < n; i++)
            {
                if (input[i] != ' ')
                {

                    char s = input[i];
                    if (input[i] == 'j')
                    {
                        s = 'i';
                    }

                    if (table.ContainsKey(s))
                    {
                        firstCoordinates[cordinatesCounter] = int.Parse(table[s].ToString()) / 10;
                        secondCoordinates[cordinatesCounter] = int.Parse(table[s].ToString()) % 10;
                        cordinatesCounter++;
                    }
                }
            }
            int firstIndex = 0, secondIndex = 0;
            while (secondIndex < cordinatesCounter)
            {
                int k = 5;
                if (firstIndex == cordinatesCounter - cordinatesCounter % 5)
                {
                    k = cordinatesCounter % 5;
                }
                for (int i = 0; i < k; i++)
                {
                    cryptedCoordinats[cryptedCounter] = firstCoordinates[firstIndex];
                    firstIndex++;
                    cryptedCounter++;
                }
                for (int i = 0; i < k; i++)
                {
                    cryptedCoordinats[cryptedCounter] = secondCoordinates[secondIndex];
                    secondIndex++;
                    cryptedCounter++;
                }
            }
            for (int i = 0; i < cryptedCounter; i += 2)
            {
                int tmp = 10 * cryptedCoordinats[i] + cryptedCoordinats[i + 1];
                string strKey = tmp.ToString();
                output += table[strKey];
            }
            return output;
        }
        private string BifidDecrypt(string input, Hashtable table)
        {
            string output = "";
            int n = input.Length;
            int[] cryptedCoordinats = new int[2 * n];
            int[] firstCoordinates = new int[n];
            int[] secondCoordinates = new int[n];
            int cordinatesCounter = 0;


            for (int i = 0; i < n; i++)
            {
                char s = input[i];
                if (table.ContainsKey(s))
                {
                    cryptedCoordinats[cordinatesCounter] = int.Parse(table[s].ToString()) / 10;
                    cordinatesCounter++;
                    cryptedCoordinats[cordinatesCounter] = int.Parse(table[s].ToString()) % 10;
                    cordinatesCounter++;
                }
            }

            for (int i = 0; i < 2 * n - (2 * n) % 10; i += 10)
            {
                for (int j = 0; j < 5; j++)
                {
                    firstCoordinates[i / 2 + j] = cryptedCoordinats[i + j];
                    secondCoordinates[i / 2 + j] = cryptedCoordinats[i + 5 + j];
                }
            }

            int k = 2 * n - (2 * n) % 10;
            for (int i = 0; i < (2 * n) % 10 / 2; i++)
            {
                firstCoordinates[k / 2 + i] = cryptedCoordinats[k + i];
                secondCoordinates[k / 2 + i] = cryptedCoordinats[k + (2 * n) % 10 / 2 + i];
            }

            for (int i = 0; i < n; i++)
            {
                string key = (firstCoordinates[i] * 10 + secondCoordinates[i]).ToString();
                if (table.ContainsKey(key))
                    output += table[key];
            }
            return output;
        }

        ///////////////////////////////////////////////////////////////////
        ///
        void rotiraj(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            uint Temp = a;
            a = b;
            b = c;
            c = d;
            d = Temp;
        }
        private uint ToUintFromHexFromString(string s)
        {
            byte[] littleEndianBytes = BitConverter.GetBytes(
                    uint.Parse(s, System.Globalization.NumberStyles.HexNumber));
            string l = BitConverter.ToString(littleEndianBytes).Replace("-", "");
            uint A = Convert.ToUInt32(l, 16);
            return A;
        }
        private uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (configData.W - count));
        }
        private uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }
        static string HexToString(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            string result = Encoding.UTF8.GetString(bytes);
            return result;
        }
        ///

        private void cbCrypted_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCrypted.Checked)
            {
                lbCryptedMessages.Visible = true;
            }
            else
            {
                lbCryptedMessages.Visible = false;
            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboBox1.SelectedIndex == 0)
            {
                pickedBifid = true;
                picked = true;
                btnSend.Enabled = true;
                btnSendFile.Enabled = true;
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                pickedBifid = false;
                picked = true;
                btnSend.Enabled = true;
                btnSendFile.Enabled = true;
            }
        }
        private void helpLabel_MouseHover(object sender, EventArgs e)
        {
            helpRichTxtBox.Visible = true;
        }
        private void helpLabel_MouseLeave(object sender, EventArgs e)
        {
            helpRichTxtBox.Visible = false;
        }

    }
}
