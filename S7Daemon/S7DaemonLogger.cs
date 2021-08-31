using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace S7Console
{
    class S7DaemonLogger
    {
        // S7Daemon logger
        // Coding by kjurlina. Have a lot of fun
        // Application logging
        private string LogFilePath;
        private string FullLogFilePath;
        private string LogFileName;
        private string LogFileExtension;

        private string ConfigFilePath;
        private string FullConfigFilePath;
        private string ConfigFileName;
        private string ConfigFileExtension;

        public S7DaemonLogger(string logPath, string logName, string configPath, string configName)
        {
            // Compose log file path, name and exitension
            LogFilePath = logPath;
            LogFileName = logName;
            LogFileExtension = ".txt";
            FullLogFilePath = LogFilePath + @"\" + LogFileName + LogFileExtension;

            // Compose log file path, name and exitension
            ConfigFilePath = configPath;
            ConfigFileName = configName;
            ConfigFileExtension = ".txt";
            FullConfigFilePath = ConfigFilePath + @"\" + ConfigFileName + ConfigFileExtension;
        }

        public bool CheckLogFileExistence()
        {
            // Check if log file exists         
            return File.Exists(FullLogFilePath);
        }

        public long ChecklLogFileSize()
        {
            // Check log file size
            long LogFileSize = new FileInfo(FullLogFilePath).Length;
            return LogFileSize;
        }

        public void CreateLogFile()
        {
            // Create log file
            var fileStream = File.Create(FullLogFilePath);
            fileStream.Close();
        }

        public void ArchiveLogFile()
        {
            // Save file and extend name with current timestamp
            string FullArchiveLogFilePath;
            string LogFileTS = DateTime.Now.Year.ToString() + "_" +
                                DateTime.Now.Month.ToString() + "_" +
                                DateTime.Now.Day.ToString() + "_" +
                                DateTime.Now.Hour.ToString() + "_" +
                                DateTime.Now.Minute.ToString() + "_" +
                                DateTime.Now.Second.ToString();

            FullArchiveLogFilePath = LogFilePath + @"\" + LogFileName + "_" + LogFileTS + LogFileExtension;

            File.Copy(FullLogFilePath, FullArchiveLogFilePath);
            File.Delete(FullLogFilePath);
        }

        public bool CheckConfigFileExistence()
        {
            // Check if config file exists
            return File.Exists(FullConfigFilePath);
        }

        public string CheckConfigFileName()
        {
            // Return value for manipulation in main program
            return FullConfigFilePath;
        }

        public void ToConsole(string message, DateTime jiffy)
        {
            // Output message to console
            Console.WriteLine(jiffy.ToString() + " :: " + message);
        }

        public void ToLogFile(string message, DateTime jiffy)
        {
            // Output message to log file
            using (StreamWriter sw = File.AppendText(FullLogFilePath))
            {
                sw.WriteLine(jiffy.ToString() + " :: " + message);
            }
        }
    }
}
