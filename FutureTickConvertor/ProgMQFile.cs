using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using System.IO;

namespace FutureTickConvertor
{
    public sealed class ProgMqFile
    {
        Dictionary<string, DateTime> _beginlist = new Dictionary<string, DateTime>();
        Dictionary<string, DateTime> _endlist = new Dictionary<string, DateTime>();

        /// <summary>
        /// 如果prog.mq文件中没有找到对应的合约，返回的开始时间和结束时间 由原来的default(DateTime)改成startTime
        /// </summary>
        private DateTime _startTime=default(DateTime);

        public ProgMqFile(string strFile, DateTime startTime=default(DateTime))
        {
            try
            {
                this._startTime = startTime;
                using (FileStream stream = new FileStream(strFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    while (!reader.EndOfStream)
                    {
                        string strline = reader.ReadLine().Trim();
                        string[] strlist = strline.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (strlist.Length == 3)
                        {
                            if (_beginlist.ContainsKey(strlist[0]))
                            {
                                throw new Exception("重复项: " + strlist[0]);
                            }

                            DateTime begintime = DateTime.ParseExact(strlist[1], "yyyy-MM-dd", null);
                            DateTime endtime = DateTime.ParseExact(strlist[2], "yyyy-MM-dd", null);

                            _beginlist.Add(strlist[0], begintime);
                            _endlist.Add(strlist[0], endtime);
                        }
                    }

                    reader.Close();
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("读取文件失败(" + strFile + "): " + ex.Message);
            }
        }

        public DateTime BeginTime(string key)
        {
            if (_beginlist.ContainsKey(key))
                return _beginlist[key];

           // return default(DateTime);
            return _startTime;
        }

        public DateTime EndTime(string key)
        {
            if (_endlist.ContainsKey(key))
                return _endlist[key];

           // return default(DateTime);
            return _startTime;
        }

        public bool Contains(string key)
        {
            return _beginlist.ContainsKey(key);
        }

        public void Add(string key, DateTime begin, DateTime end)
        {
            if (_beginlist.ContainsKey(key))
            {
                _beginlist[key] = begin;
                _endlist[key] = end;
            }
            else
            {
                _beginlist.Add(key, begin);
                _endlist.Add(key, end);
            }
        }

        public void Remove(string key)
        {
            if (_beginlist.ContainsKey(key))
            {
                _beginlist.Remove(key);
            }

            if (_endlist.ContainsKey(key))
            {
                _endlist.Remove(key);
            }
        }

        public void Save(string strFile)
        {
            try
            {
                using (FileStream stream = new FileStream(strFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
                    foreach(string key in _beginlist.Keys)
                    {
                        if (_endlist.ContainsKey(key))
                        {
                            writer.Write(key);
                            writer.Write(",");
                            writer.Write(_beginlist[key].ToString("yyyy-MM-dd"));
                            writer.Write(",");
                            writer.Write(_endlist[key].ToString("yyyy-MM-dd"));
                            writer.WriteLine();
                        }
                        else
                        {
                            writer.Write(key);
                            writer.Write(",");
                            writer.Write(_beginlist[key].ToString("yyyy-MM-dd"));
                            writer.Write(",");
                            writer.Write(_beginlist[key].ToString("yyyy-MM-dd"));
                            writer.WriteLine();
                        }
                    }

                    writer.Close();
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("保存文件失败(" + strFile + "): " + ex.Message);
            }
        }
    }
}
