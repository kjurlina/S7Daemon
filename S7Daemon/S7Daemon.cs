using System;
using System.IO;
using System.Threading.Tasks;
using Sharp7;

namespace S7Console
{
    // S7Daemon
    // Coding by kjurlina. Have a lot of fun
    class S7Daemon
    {
        static async Task Main(string[] args)
        {
            // Global project variables
            string FullConfigFilePath;
            string DefaultConfigFileName = "S7DaemonConfig";
            string DefaultConfigFilePath;
            string DefaultLogFileName = "S7DaemonLog";
            string DefaultLogFilePath;
            string DefaultDBFileName = "S7DaemonDB.sqlite";
            string DefaultDBFilePath;
            string[] ConfigFileContent;
            string[] ConfigLineContent;
            string SeparationString = " :: ";
            string PLC_IPAddress = "";
            string PLC_Rack = "";
            string PLC_Slot = "";
            string Report_Folder;
            string[,] LoggingTags = new string[65535, 8];
            int ConfigFileNumberOfLines = 0;
            int ConfigFileNumberOfTags = 0;
            int i;

            // Global S7 variables
            S7Client Client;
            byte[] Buffer = new byte[4];
            byte[] BufferBigEndian = new byte[4];
            int ReadArea;
            int ReadDB;
            int ReadStart;
            int ReadAmount;
            int ReadWordLen;
            int ReadType;
            int ReadTempo;
            int ConnectionResult = -1;
            int ReadResult = -1;
            bool[] ReportingInterval = new bool[14];

            // Create configuration & log file names & paths
            if (Environment.OSVersion.ToString().Contains("Windows"))
            {
                DefaultLogFilePath = @"D:\S7Daemon\log";
                DefaultConfigFilePath = @"D:\S7Daemon\config";
                DefaultDBFilePath = @"D:\S7Daemon\db";
            }
            else if (Environment.OSVersion.Platform.ToString().Contains("Linux"))
            {
                DefaultLogFilePath = "";
                DefaultConfigFilePath = "";
                DefaultDBFilePath = "";
            }
            else
            {
                DefaultLogFilePath = "";
                DefaultConfigFilePath = "";
                DefaultDBFilePath = "";
               Console.WriteLine("Unsupported OS version. Exiting application...");
                return;
            }

            // Create cron & logger instance
            S7DaemonCron Cron = new S7DaemonCron();
            S7DaemonLogger Logger = new S7DaemonLogger(DefaultLogFilePath, DefaultLogFileName, DefaultConfigFilePath, DefaultConfigFileName);

            // Check if log file exists. If not create one
            if (!Logger.CheckLogFileExistence())
            {
                Logger.CreateLogFile();
                Logger.ToLogFile("Application started", DateTime.Now);
                Logger.ToLogFile("New Log file created", DateTime.Now);
            }
            // If ilog file exists check its size. If file is too big (>1MB) archive it and create new one
            else if(Logger.ChecklLogFileSize()>1048576)
            {
                Logger.ToLogFile("Log file size is " + Logger.ChecklLogFileSize().ToString() + " bytes", DateTime.Now);
                Logger.ToLogFile("That is a bit too much. This log file will be archived and new one will be created", DateTime.Now);
                Logger.ArchiveLogFile();
                Logger.CreateLogFile();
            }
            else
            {
                Logger.ToLogFile("Application started", DateTime.Now);
            }

            // Event handler for application closing event
            AppDomain.CurrentDomain.ProcessExit += new EventHandler((sender, e) => CurrentDomain_ProcessExit(sender, e, Logger));

            // Check if database exists. If not create one
            S7DaemonSQLite SQLite = new S7DaemonSQLite(DefaultDBFilePath, DefaultDBFileName);
            try
            {
                if (!SQLite.CheckDatabaseExists())
                {
                    SQLite.CreateDatabase();
                    Logger.ToLogFile("A brand new database has been created", DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile(ex.Message, DateTime.Now);
                return;
            }

            // Check if config file exists. If not exit application
            try
            {
                FullConfigFilePath = Logger.CheckConfigFileName();
            }
            catch (Exception ex)
            {
                Logger.ToLogFile(ex.Message, DateTime.Now);
                return;
            }

            // Read configuration file content
            try
            {
                ConfigFileContent = File.ReadAllLines(FullConfigFilePath);
                ConfigFileNumberOfLines = File.ReadAllLines(FullConfigFilePath).Length;
                Logger.ToLogFile("Configuration file loaded. Number of lines is " + ConfigFileNumberOfLines.ToString(), DateTime.Now);

                // Get PLC IP Address
                i = 0;
                while (i <= ConfigFileNumberOfLines)
                {
                    if (i >= ConfigFileNumberOfLines)
                    {
                        Logger.ToLogFile("PLC IP Address not found in configuration file. Exiting application...", DateTime.Now);
                        return;
                    }

                    ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                    if (ConfigLineContent[0] == "PLC_Address")
                    {
                        PLC_IPAddress = ConfigLineContent[1];
                        Logger.ToLogFile("Configured PLC IP Address is " + PLC_IPAddress, DateTime.Now);
                        break;
                    }

                    i++;
                }

                // Get PLC Rack
                i = 0;
                while (i <= ConfigFileNumberOfLines)
                {
                    if (i >= ConfigFileNumberOfLines)
                    {
                        Logger.ToLogFile("PLC Rack not found in configuration file. Exiting application...", DateTime.Now);
                        return;
                    }

                    ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                    if (ConfigLineContent[0] == "PLC_Rack")
                    {
                        PLC_Rack = ConfigLineContent[1];
                        Logger.ToLogFile("Configured PLC Rack is " + PLC_Rack, DateTime.Now);
                        break;
                    }

                    i++;
                }

                // Get PLC Slot
                i = 0;
                while (i <= ConfigFileNumberOfLines)
                {
                    if (i >= ConfigFileNumberOfLines)
                    {
                        Logger.ToLogFile("PLC Slot not found in configuration file. Exiting application...", DateTime.Now);
                        return;
                    }

                    ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                    if (ConfigLineContent[0] == "PLC_Slot")
                    {
                        PLC_Slot = ConfigLineContent[1];
                        Logger.ToLogFile("Configured PLC Slot is " + PLC_Slot, DateTime.Now);
                        break;
                    }

                    i++;
                }

                // Get tags
                i = 0;
                while (i < ConfigFileNumberOfLines)
                {
                    ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                    if (ConfigLineContent[0] == "PLC_Tag")
                    {
                        ConfigFileNumberOfTags++;
                        // Tag number
                        LoggingTags[ConfigFileNumberOfTags, 0] = (i+1).ToString();
                        // Tag name
                        LoggingTags[ConfigFileNumberOfTags, 1] = ConfigLineContent[1];
                        // Tag area
                        if (ConfigLineContent[2] == "DB")
                        {
                            LoggingTags[ConfigFileNumberOfTags, 2] = "132";
                        }
                        else if (ConfigLineContent[2] == "M")
                        {
                            LoggingTags[ConfigFileNumberOfTags, 2] = "131";
                        }
                        else
                        {
                            Logger.ToLogFile("Misconfigured tag area " + ConfigFileNumberOfTags + ". Exiting application...", DateTime.Now);
                            return;
                        }
                        // Tag DB Number
                        if (ConfigLineContent[2] == "DB")
                        {
                            LoggingTags[ConfigFileNumberOfTags, 3] = ConfigLineContent[3];
                        }
                        else
                        {
                            LoggingTags[ConfigFileNumberOfTags, 3] = "0";
                        }
                        // Tag begin
                        LoggingTags[ConfigFileNumberOfTags, 4] = ConfigLineContent[4].ToString();
                        // Tag length & type
                        if (ConfigLineContent[5] == "INT")
                        {
                            LoggingTags[ConfigFileNumberOfTags, 5] = "2";
                            LoggingTags[ConfigFileNumberOfTags, 6] = "1";
                        }
                        else if (ConfigLineContent[5] == "DINT")
                        {
                            LoggingTags[ConfigFileNumberOfTags, 5] = "4";
                            LoggingTags[ConfigFileNumberOfTags, 6] = "2";
                        }
                        else if (ConfigLineContent[5] == "REAL")
                        {
                            LoggingTags[ConfigFileNumberOfTags, 5] = "4";
                            LoggingTags[ConfigFileNumberOfTags, 6] = "3";
                        }
                        else
                        {
                            Logger.ToLogFile("Misconfigured tag type " + ConfigFileNumberOfTags + ". Exiting application...", DateTime.Now);
                            return;
                        }
                        // Tag logging tempo
                        LoggingTags[ConfigFileNumberOfTags, 7] = ConfigLineContent[6];
                    }
                    i++;
                }

                if (ConfigFileNumberOfTags < 1)
                {
                    Logger.ToLogFile("No tags found in configuration file. Exiting application...", DateTime.Now);
                    return;
                }
                else
                {
                    Logger.ToLogFile("Configured number of tags is " + ConfigFileNumberOfTags.ToString(), DateTime.Now);
                }

                // Get report folder path
                i = 0;
                while (i <= ConfigFileNumberOfLines)
                {
                    if (i >= ConfigFileNumberOfLines)
                    {
                        Logger.ToLogFile("Report folder path not found in configuration file. Exiting application...", DateTime.Now);
                        return;
                    }

                    ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                    if (ConfigLineContent[0] == "Report_Folder")
                    {
                        Report_Folder = ConfigLineContent[1];
                        Logger.ToLogFile("Report folder path is " + Report_Folder, DateTime.Now);
                        break;
                    }

                    i++;
                }



            }
            catch (Exception ex)
            {
                Logger.ToLogFile(ex.Message + " Exiting application...", DateTime.Now);
            }

            // Create database tables if they do not exist
            for (i = 1; i <= ConfigFileNumberOfTags; i++)
            {
                try
                {
                    if (!SQLite.CheckTableExists(LoggingTags[i, 1]))
                    {
                        SQLite.CreateTable(LoggingTags[i, 1], LoggingTags[i, 6]);
                        Logger.ToLogFile("Table " + LoggingTags[i, 1] + " created", DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ToLogFile(ex.Message, DateTime.Now);
                }
            }

            // Create S7 client instance
            Client = new S7Client();            

            // Main connect/read/diconnect loop
            try
            {
                while (true)
                {
                    // Connect to PLC
                    await Connect();

                    // If connected read PLC variables
                    if (ConnectionResult == 0)
                    {
                        await Read();
                    }

                    // Function to connect to PLC
                    async Task Connect()
                    {
                        while (ConnectionResult != 0)
                        {
                            ConnectionResult = Client.ConnectTo(PLC_IPAddress, Convert.ToInt32(PLC_Rack), Convert.ToInt32(PLC_Slot));
                            if (ConnectionResult == 0)
                            {
                                Logger.ToConsole("Successfully connected to PLC", DateTime.Now);
                                Logger.ToLogFile("Successfully connected to PLC", DateTime.Now);
                            }
                            else
                            {
                                Logger.ToLogFile("Could not connect to PLC... Trying again in 5 seconds", DateTime.Now);
                                await Task.Delay(5000);
                            }
                        }
                    }

                    // Function to read PLC variables (variable tempo)
                    async Task Read()
                    {
                        while (ReadResult == 0 | ReadResult == -1)
                        {
                            // Read current time and store (for consistent messaging)
                            DateTime Jiffy = DateTime.Now;

                            // Get reporting interval array (from cron)
                            ReportingInterval = Cron.GetReportingInterval(Jiffy);

                            // Loop trough all tags and report if necessary
                            i = 1;

                            while (i <= ConfigFileNumberOfTags & (ReadResult == 0 | ReadResult == -1))
                            {
                                // Tag area
                                ReadArea = Convert.ToInt32(LoggingTags[i, 2]);
                                // DB Number
                                ReadDB = Convert.ToInt32(LoggingTags[i, 3]);
                                // Starting byte (offset)
                                ReadStart = Convert.ToInt32(LoggingTags[i, 4]);
                                // Readint length (in bytes)
                                ReadWordLen = Convert.ToInt32(LoggingTags[i, 5]);
                                // Data type to be read
                                ReadType = Convert.ToInt32(LoggingTags[i, 6]);
                                // Reading amount - not really sure is this bytes or words (it works with 2 ?!)
                                ReadAmount = 2;
                                // Reading tempo
                                ReadTempo = Convert.ToInt32(LoggingTags[i, 7]);

                                // Check if current tag qualifies for reporting
                                if (Cron.CheckReportingInterval(ReportingInterval, ReadTempo))
                                {
                                    // Read value
                                    ReadResult = Client.ReadArea(ReadArea, ReadDB, ReadStart, ReadAmount, ReadWordLen, Buffer);                                    

                                    // Interpret the resut
                                    if (ReadResult == 0)
                                    {
                                        if (ReadType == 1)
                                        {
                                            // Integer
                                            BufferBigEndian[0] = Buffer[1];
                                            BufferBigEndian[1] = Buffer[0];
                                            BufferBigEndian[2] = 0;
                                            BufferBigEndian[3] = 0;
                                            SQLite.InsertIntoTable(LoggingTags[i, 1], BitConverter.ToUInt32(BufferBigEndian), Jiffy);
                                        }
                                        else if (ReadType == 2)
                                        {
                                            // DINT
                                            BufferBigEndian[0] = Buffer[3];
                                            BufferBigEndian[1] = Buffer[2];
                                            BufferBigEndian[2] = Buffer[1];
                                            BufferBigEndian[3] = Buffer[0];
                                            SQLite.InsertIntoTable(LoggingTags[i, 1], BitConverter.ToUInt32(BufferBigEndian), Jiffy);
                                        }
                                        else if (ReadType == 3)
                                        {
                                            // REAL
                                            BufferBigEndian[0] = Buffer[3];
                                            BufferBigEndian[1] = Buffer[2];
                                            BufferBigEndian[2] = Buffer[1];
                                            BufferBigEndian[3] = Buffer[0];
                                            SQLite.InsertIntoTable(LoggingTags[i, 1], BitConverter.ToSingle(BufferBigEndian), Jiffy);                                            
                                        }
                                        else
                                        {
                                            Logger.ToLogFile("Something went wrong while reading tag " + LoggingTags[i, 1] + ". Not writing to database", DateTime.Now);
                                        }
                                    }
                                }
                                i++;
                            }
                            await Task.Delay(TimeSpan.FromMilliseconds(400));                                                       
                        }
                    }

                    // Warn that something has happened
                    Logger.ToConsole("Connection to PLC has been interrupted. Trying to reconnect...", DateTime.Now);
                    Logger.ToLogFile("Connection to PLC has been interrupted. Trying to reconnect...", DateTime.Now);

                    // Try to reconnect
                    try
                    {
                        Client.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Logger.ToLogFile(ex.Message, DateTime.Now);
                    }

                    ConnectionResult = -1;
                    ReadResult = -1;
                    await Task.Delay(0);
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile(ex.Message, DateTime.Now);
            }

            static void CurrentDomain_ProcessExit(object sender, EventArgs e, S7DaemonLogger logger)
            {
                logger.ToLogFile("Application closed", DateTime.Now);
                logger.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++", DateTime.Now);
            }
        }
    }
}
