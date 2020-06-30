using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FutureTickConvertor
{
	//夜盘 期货 时间帮助器
    public static class YfTimeHelper
    {
        /// <summary>
        /// true: t1 > t2
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="midtime"></param>
        /// <returns></returns>
        public static bool IsLarger(TimeSpan t1, TimeSpan t2, TimeSpan midtime)
        {
            if (t1 < midtime)
            {
                if (t2 < midtime)
                {
                    return t1 > t2;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (t2 < midtime)
                {
                    return false;
                }
                else
                {
                    return t1 > t2;
                }
            }
        }

        /// <summary>
        /// true: t1 >= t2
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="midtime"></param>
        /// <returns></returns>
        public static bool IsLargerOrEqual(TimeSpan t1, TimeSpan t2, TimeSpan midtime)
        {
            if (t1 < midtime)
            {
                if (t2 < midtime)
                {
                    return t1 >= t2;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (t2 < midtime)
                {
                    return false;
                }
                else
                {
                    return t1 >= t2;
                }
            }
        }

        /// <summary>
        /// true : t2 > t > t1
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t"></param>
        /// <param name="midtime"></param>
        /// <returns></returns>
        public static bool IsBetween00(TimeSpan t1, TimeSpan t2, TimeSpan t, TimeSpan midtime)
        {
            if (t < midtime)
            {
                if (t2 > midtime)
                    return false;

                if (t2 <= t)
                    return false;

                if (t1 < midtime)
                {
                    return t1 < t;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (t1 < midtime)
                    return false;

                if (t1 >= t)
                    return false;

                if (t2 < midtime)
                {
                    return true;
                }
                else
                {
                    return t < t2;
                }
            }
        }

        /// <summary>
        /// t2 > t >= t1
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t"></param>
        /// <param name="midtime"></param>
        /// <returns></returns>
        public static bool IsBetween10(TimeSpan t1, TimeSpan t2, TimeSpan t, TimeSpan midtime)
        {
            if (t < midtime)
            {
                if (t2 > midtime)
                    return false;

                if (t2 <= t)
                    return false;

                if (t1 < midtime)
                {
                    return t1 <= t;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (t1 < midtime)
                    return false;

                if (t1 > t)
                    return false;

                if (t2 < midtime)
                {
                    return true;
                }
                else
                {
                    return t < t2;
                }
            }
        }

        /// <summary>
        /// t2 >= t >= t1
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t"></param>
        /// <param name="midtime"></param>
        /// <returns></returns>
        public static bool IsBetween11(TimeSpan t1, TimeSpan t2, TimeSpan t, TimeSpan midtime)
        {
            if (t < midtime)
            {
                if (t2 > midtime)
                    return false;

                if (t2 < t)
                    return false;

                if (t1 < midtime)
                {
                    return t1 <= t;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (t1 < midtime)
                    return false;

                if (t1 > t)
                    return false;

                if (t2 < midtime)
                {
                    return true;
                }
                else
                {
                    return t <= t2;
                }
            }
        }

        static TimeSpan _oneday = new TimeSpan(1, 0, 0, 0);

        /// <summary>
        /// t1 - t2
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static TimeSpan Distance(TimeSpan t1, TimeSpan t2, TimeSpan midtime)
        {
            TimeSpan t = t1 - t2;
            if (t < default(TimeSpan))
            {
                if (t1 < midtime && t2 > midtime)
                    return t.Add(_oneday);
            }

            return t;
        }

        public static DateTime JoinDateTime(DateTime tradingday, DateTime pretradingday1, DateTime pretradingday2, TimeSpan timeSpan)
        {
            if (timeSpan.Hours < 7)
            {
                return pretradingday2 + timeSpan;
            }
            else if (timeSpan.Hours < 18)
            {
                return tradingday + timeSpan;
            }
            else
            {
                return pretradingday1 + timeSpan;
            }
        }

        public static DateTime GetTradingDay(DateTime datetime)
        {
            if (datetime.Hour < 7)
            {
                if (datetime.DayOfWeek != DayOfWeek.Saturday)
                    return datetime.Date;
                else
                    return datetime.AddDays(2).Date;
            }
            else if (datetime.Hour < 18)
            {
                return datetime.Date;
            }
            else
            {
                if (datetime.DayOfWeek != DayOfWeek.Friday)
                    return datetime.AddDays(1).Date;
                else
                    return datetime.AddDays(3).Date;
            }
        }

        public static DateTime GetDateTimeByTradingDay(DateTime tradingday, TimeSpan timeSpan)
        {
            if (timeSpan.Hours < 7)
            {
                if (tradingday.DayOfWeek != DayOfWeek.Monday)
                    return tradingday.Date + timeSpan;
                else
                    return tradingday.Date.AddDays(-2) + timeSpan;
            }
            else if (timeSpan.Hours < 18)
            {
                return tradingday.Date + timeSpan;
            }
            else
            {
                if (tradingday.DayOfWeek != DayOfWeek.Monday)
                    return tradingday.Date.AddDays(-1) + timeSpan;
                else
                    return tradingday.Date.AddDays(-3) + timeSpan;
            }
        }
    }
}
