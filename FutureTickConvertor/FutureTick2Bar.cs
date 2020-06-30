using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ats.Core;

namespace FutureTickConvertor
{
    public partial class FutureTick2Bar : Form
    {

        #region 业务逻辑
        //将期货Tick转化成分钟线 和 日线        
        //【1】如果有prog.mq文件，系统自动根据prog.mq 里面的日期自动增量转换
        //【2】如果没有prog.mq文件，系统根据界面UI--日期控件的日期起点【默认值2010-1-1】开始转换

        #endregion

        #region 代码

        /// <summary>
        /// Tick目录
        /// </summary>
        private string _tickDir = "";

        /// <summary>
        /// 分钟线目录
        /// </summary>
        private string _minDir = "";

        /// <summary>
        /// 日线目录
        /// </summary>
        private string _dayDir = "";

        private string _configDir = "";

        /// <summary>
        /// 如果没有prog.mq文件，转换的默认开始时间
        /// </summary>
        private DateTime _startTime;

        #region 文本框操作

        private void AddTextIntoTextBox(string str)
        {
            txtInfo.AppendText(str + "\r\n");
            txtInfo.Focus();
            //光标移到最后一行末尾
            txtInfo.SelectionStart = txtInfo.TextLength - 1;
        }

        void Print(string msg)
        {
            txtInfo.BeginInvoke(new AddTextToTextBox(AddTextIntoTextBox), msg);
        }

        public void EnableButton()
        {
            btnStart.Enabled = true;
        }

        #endregion

        public FutureTick2Bar()
        {
            InitializeComponent();
        }

        private void Tick2Bar_Load(object sender, EventArgs e)
        {
            txtTargetDay.Text = _dayDir;
            txtTickDir.Text = _tickDir;
            txtTargetMin.Text = _minDir;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _minDir = txtTargetMin.Text;
            _tickDir = txtTickDir.Text;
            _dayDir = txtTargetDay.Text;
            _configDir = txtTargetConfig.Text;

            _startTime = chkStartDate.Value;

            if (Directory.Exists(_tickDir))
            {
                btnStart.Enabled = false;
                Print("开始处理数据");
                myTabs.SelectedIndex = 0;
                var ts = new ThreadStart(处理数据);
                Thread t = new Thread(ts);
                t.Start();
            }
            else
            {
                Print("Tick目录不存在[" + _tickDir + "]");
            }

        }


        void 处理数据()
        {

            Print("期货Tick目录=[" + _tickDir + "]");
            Print("期货分钟线目录=[" + _minDir + "]");
            Print("期货日线目录=[" + _dayDir + "]");
            Print("配置文件目录=[" + _configDir + "]");


            Print("开始转分钟线******************************");
            var futureMinBarConverter = new FutureMinBarConverter { AllBlankBar = chkFixBar.Checked };
            try
            {
                futureMinBarConverter.Convert(_tickDir, _minDir, _configDir, _startTime);
                Print("期货Tick转化分钟线完成!**********************");
            }
            catch (Exception ex)
            {
                Print("期货Tick转化分钟线出现异常:" + ex.Message);
            }
            Print("开始转日线******************************");
            var futureDayConvert = new FutureDayConvert();
            try
            {
                futureDayConvert.Convert(_tickDir, _dayDir, _startTime);
                Print("期货Tick转化日线完成!**********************");
            }
            catch (Exception ex)
            {
                Print("期货Tick转化日线出现异常:" + ex.Message);
            }
        }
        private void btnDeleteProgMQ_Click(object sender, EventArgs e)
        {
            //删除历史转换记录
            //历史转换记录指的是:分钟线和日线目录下的 prog.mq文件 
            string progName = _minDir + "\\prog.mq";

            if (!File.Exists(progName))
            {
                Print(progName + "文件不存在");
            }
            else
            {
                File.Delete(progName);
                Print("删除" + progName);
            }

            progName = _dayDir + "\\prog.mq";
            if (!File.Exists(progName))
            {
                Print(progName + "文件不存在");
            }
            else
            {
                File.Delete(progName);
                Print("删除" + progName);
            }
        }


        #endregion



    }
}
