using System;
using System.Collections.Generic;
using System.Text;

namespace S7Console
{
    class S7DaemonCron
    {
        // A class that takes care of triggering scheduled jobs
        // Method GetReportingInterval will return a matrix of boolean (depending on time)
        // Method CheckReportingInterval will check this matrix versus given intege values

        // First declare global variables
        int currentMillisecond;
        int currentSecond;
        int currentMinute;
        int currentHour;
        int currentDay;
        int currentMonth;
        int currentYear;
        bool[] IsCondition = new bool[14];
        bool[] WasCondition = new bool[14];
        bool[] Trigger = new bool[14];
        int i;

        public S7DaemonCron()
        {
            // Reinitialize previous trigger and current time array
            for (i = 0; i <= 13; i++)
            {
                IsCondition[i] = false;
                WasCondition[i] = false;
                Trigger[i] = false;
            }
        }

        public bool[] GetReportingInterval(DateTime jiffy)
        {
            // This class will return trigger matrix as follows:
            // [1s 2s 5s 10s 30s 1min 2min 5min 10min 30min 1h 1d 1month 1y]
            // This correstponds to config file settings:
            // [1 2 5 10 30 60 120 300 600 1800 3600 86400 20000 30000]
            // Read current time
            currentMillisecond = jiffy.Millisecond;
            currentSecond = jiffy.Second;
            currentMinute = jiffy.Minute;
            currentHour = jiffy.Hour;
            currentDay = jiffy.Day;
            currentMonth = jiffy.Month;
            currentYear = jiffy.Year;

            // Reset Trigger array
            for (i = 0; i<= 13; i++)
            {
                Trigger[i] = false;
            }

            // Check condition for 1s logging interval
            if (currentMillisecond < 500)
            {
                IsCondition[0] = true;
            }
            else
            {
                IsCondition[0] = false;
            }

            // Check condition for 2s logging interval
            if (currentSecond % 2 == 0)
            {
                IsCondition[1] = true;
            }
            else
            {
                IsCondition[1] = false;
            }

            // Check condition for 5s logging interval
            if (currentSecond % 5 == 0)
            {
                IsCondition[2] = true;
            }
            else
            {
                IsCondition[2] = false;
            }

            // Check condition for 10s logging interval
            if (currentSecond % 10 == 0)
            {
                IsCondition[3] = true;
            }
            else
            {
                IsCondition[3] = false;
            }


            // Check condition for 30s logging interval
            if (currentSecond % 30 == 0)
            {
                IsCondition[4] = true;
            }
            else
            {
                IsCondition[4] = false;
            }

            // Check condition for 1m logging interval
            if (currentSecond == 0)
            {
                IsCondition[5] = true;
            }
            else
            {
                IsCondition[5] = false;
            }

            // Check condition for 2m logging interval
            if (currentSecond == 0 & DateTime.Now.Minute % 2 == 0)
            {
                IsCondition[6] = true;
            }
            else
            {
                IsCondition[6] = false;
            }

            // Check condition for 5m logging interval
            if (currentSecond == 0 & currentMinute % 5 == 0)
            {
                IsCondition[7] = true;
            }
            else
            {
                IsCondition[7] = false;
            }

            // Check condition for 10m logging interval
            if (currentSecond == 0 & currentMinute % 10 == 0)
            {
                IsCondition[8] = true;
            }
            else
            {
                IsCondition[8] = false;
            }

            // Check condition for 30m logging interval
            if (currentSecond == 0 & currentMinute % 30 == 0)
            {
                IsCondition[9] = true;
            }
            else
            {
                IsCondition[9] = false;
            }

            // Check condition for 1h logging interval
            if (currentSecond == 0 & currentMinute == 0)
            {
                IsCondition[10] = true;
            }
            else
            {
                IsCondition[10] = false;
            }

            // Check condition for 1d logging interval
            if (currentSecond == 0 & currentMinute == 0 & currentHour == 0)
            {
                IsCondition[11] = true;
            }
            else
            {
                IsCondition[11] = false;
            }

            // Check condition for 1m logging interval
            if (currentSecond == 0 & currentMinute == 0 & currentHour == 0 & currentDay == 1)
            {
                IsCondition[12] = true;
            }
            else
            {
                IsCondition[12] = false;
            }

            // Check condition for 1y logging interval
            if (currentSecond == 0 & currentMinute == 0 & currentHour == 0 & currentDay == 1 & currentMonth == 1)
            {
                IsCondition[13] = true;
            }
            else
            {
                IsCondition[13] = false;
            }

            // Create trigger array
            for (i = 0; i <= 13; i++)
            {
                if (IsCondition[i] & !WasCondition[i])
                {
                    Trigger[i] = true;
                    WasCondition[i] = true;
                }
                else if (!IsCondition[i] & WasCondition[i])
                {
                    WasCondition[i] = false;
                }
            }

            return Trigger;
        }

        public bool CheckReportingInterval(bool[] Trigger, int Interval)
        {
            // This method will return true if requested reporting interval has just happened
            // Please see GetReportingInterval method

            // Declare variables
            bool IsChecked;

            // Check interval versus trigger matrix
            switch(Interval)
            {
                case 1: // 1second interval
                    IsChecked = Trigger[0];
                    break;
                case 2: // 2 second interval
                    IsChecked = Trigger[1];
                    break;
                case 5: // 5 second interval
                    IsChecked = Trigger[2];
                    break;
                case 10: // 10 second interval
                    IsChecked = Trigger[3];
                    break;
                case 30: // 30 second interval
                    IsChecked = Trigger[4];
                    break;
                case 60: // 1 minute interval
                    IsChecked = Trigger[5];
                    break;
                case 120: // 2 minute interval
                    IsChecked = Trigger[6];
                    break;
                case 300: // 5 minute interval
                    IsChecked = Trigger[7];
                    break;
                case 600: // 10 minute interval
                    IsChecked = Trigger[8];
                    break;
                case 1800: // 30 minute interval
                    IsChecked = Trigger[9];
                    break;
                case 3600: // 1 hour interval
                    IsChecked = Trigger[10];
                    break;
                case 86400: // 1 day interval
                    IsChecked = Trigger[11];
                    break;
                case 20000: // 1 month interval
                    IsChecked = Trigger[12];
                    break;
                case 30000: // 1 year interval
                    IsChecked = Trigger[13];
                    break;
                default:
                    IsChecked = false;
                    break;
            }

            return IsChecked;

        }

    }
}
