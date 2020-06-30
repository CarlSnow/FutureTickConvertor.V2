using Ats.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Office.Extened.Csv;

namespace WpfTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void MqDataTools_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            CmbBox.ItemsSource = System.Enum.GetValues(typeof(EDataType));
            CmbBox.SelectedIndex = 0;

            DpBeginDate.Text = DateTime.Today.ToLongDateString();
            DpEndDate.Text = DateTime.Today.ToLongDateString();

            MainGrid.AddHandler(ComboBox.SelectionChangedEvent, new RoutedEventHandler(SelectedItemChangedHanler));
            LoadTickLayout();
        }

        private void SelectedItemChangedHanler(object sender, RoutedEventArgs routedEventArgs)
        {
            if (routedEventArgs.Source is ComboBox)
            {
                ComboBox cmbx = (ComboBox)routedEventArgs.Source;
                if (cmbx.Name != "CmbBox") return;

                MainListView.Items.Clear();
                EDataType eDataType = (EDataType)cmbx.SelectedItem;
                switch (eDataType)
                {
                    case EDataType.Tick:
                        LoadTickLayout();
                        break;
                    case EDataType.Min:
                        LoadMinLayout();
                        break;
                    default:
                        LoadTickLayout();
                        break;
                }
            }
        }
        private void LoadTickLayout()
        {
            DpEndDate.IsEnabled = false;
            GridView gridView = new GridView();
            GridViewColumn gvcInstrumentId = new GridViewColumn() { Header = "合约代码", Width = 60, DisplayMemberBinding = new Binding("InstrumentID") };
            GridViewColumn gvcTradingDay = new GridViewColumn() { Header = "交易日", Width = 80, DisplayMemberBinding = new Binding("TradingDay") { StringFormat = "yyyyMMdd" } };
            GridViewColumn gvcNatureDay = new GridViewColumn() { Header = "自然日", Width = 80, DisplayMemberBinding = new Binding("DateTime") { StringFormat = "yyyyMMdd" } };
            GridViewColumn gvcTime = new GridViewColumn() { Header = "时间", Width = 100, DisplayMemberBinding = new Binding("TickNow") };
            GridViewColumn gvcLastPrice = new GridViewColumn() { Header = "最新价", Width = 60, DisplayMemberBinding = new Binding("LastPrice") };
            GridViewColumn gvcVolumeNow = new GridViewColumn() { Header = "现量", Width = 60, DisplayMemberBinding = new Binding("VolumeNow") };
            GridViewColumn gvcBidPrice1 = new GridViewColumn() { Header = "买1价", Width = 60, DisplayMemberBinding = new Binding("BidPrice1") };
            GridViewColumn gvcAskPrice1 = new GridViewColumn() { Header = "卖1价", Width = 60, DisplayMemberBinding = new Binding("AskPrice1") };
            GridViewColumn gvcBidVolume1 = new GridViewColumn() { Header = "买1量", Width = 60, DisplayMemberBinding = new Binding("BidVolume1") };
            GridViewColumn gvcAskVolume1 = new GridViewColumn() { Header = "卖1量", Width = 60, DisplayMemberBinding = new Binding("AskVolume1") };
            GridViewColumn gvcHighPrice = new GridViewColumn() { Header = "高", Width = 60, DisplayMemberBinding = new Binding("HighPrice") };
            GridViewColumn gvcOpenPrice = new GridViewColumn() { Header = "开", Width = 60, DisplayMemberBinding = new Binding("OpenPrice") };
            GridViewColumn gvcLowPrice = new GridViewColumn() { Header = "低", Width = 60, DisplayMemberBinding = new Binding("LowPrice") };
            GridViewColumn gvcChange = new GridViewColumn() { Header = "涨幅", Width = 60, DisplayMemberBinding = new Binding("Change") };
            GridViewColumn gvcVolume = new GridViewColumn() { Header = "成交量", Width = 60, DisplayMemberBinding = new Binding("Volume") };
            GridViewColumn gvcTurnover = new GridViewColumn() { Header = "成交额", Width = 100, DisplayMemberBinding = new Binding("Turnover") };

            gridView.Columns.Add(gvcInstrumentId);
            gridView.Columns.Add(gvcTradingDay);
            gridView.Columns.Add(gvcNatureDay);
            gridView.Columns.Add(gvcTime);
            gridView.Columns.Add(gvcLastPrice);
            gridView.Columns.Add(gvcVolumeNow);
            gridView.Columns.Add(gvcBidPrice1);
            gridView.Columns.Add(gvcAskPrice1);
            gridView.Columns.Add(gvcBidVolume1);
            gridView.Columns.Add(gvcAskVolume1);
            gridView.Columns.Add(gvcHighPrice);
            gridView.Columns.Add(gvcOpenPrice);
            gridView.Columns.Add(gvcLowPrice);
            gridView.Columns.Add(gvcChange);
            gridView.Columns.Add(gvcVolume);
            gridView.Columns.Add(gvcTurnover);
            MainListView.View = gridView;
        }

        private void LoadMinLayout()
        {
            DpEndDate.IsEnabled = true;

            GridView gridView = new GridView();
            GridViewColumn gvcTradingDay = new GridViewColumn() { Header = "交易日", Width = 80, DisplayMemberBinding = new Binding("TradingDate") { StringFormat = "yyyyMMdd" } };
            GridViewColumn gvcNatureDay = new GridViewColumn() { Header = "自然日", Width = 80, DisplayMemberBinding = new Binding("TradingDate") { StringFormat = "yyyyMMdd" } };
            GridViewColumn gvcStartTime = new GridViewColumn() { Header = "开始", Width = 80, DisplayMemberBinding = new Binding("BeginTime") { StringFormat = "HH:mm:ss" } };
            GridViewColumn gvcEndTime = new GridViewColumn() { Header = "结束", Width = 80, DisplayMemberBinding = new Binding("EndTime") { StringFormat = "HH:mm:ss" } };
            GridViewColumn gvcHighPrice = new GridViewColumn() { Header = "高", Width = 60, DisplayMemberBinding = new Binding("High") };
            GridViewColumn gvcOpenPrice = new GridViewColumn() { Header = "开", Width = 60, DisplayMemberBinding = new Binding("Open") };
            GridViewColumn gvcLowPrice = new GridViewColumn() { Header = "低", Width = 60, DisplayMemberBinding = new Binding("Low") };
            GridViewColumn gvcClosePrice = new GridViewColumn() { Header = "低", Width = 60, DisplayMemberBinding = new Binding("Close") };
            GridViewColumn gvcVolume = new GridViewColumn() { Header = "成交量", Width = 60, DisplayMemberBinding = new Binding("Volume") };
            GridViewColumn gvcTurnover = new GridViewColumn() { Header = "成交额", Width = 100, DisplayMemberBinding = new Binding("Turnover") };
            GridViewColumn gvcPositon = new GridViewColumn() { Header = "持仓量", Width = 100, DisplayMemberBinding = new Binding("Turnover") };

            gridView.Columns.Add(gvcTradingDay);
            gridView.Columns.Add(gvcNatureDay);
            gridView.Columns.Add(gvcStartTime);
            gridView.Columns.Add(gvcEndTime);
            gridView.Columns.Add(gvcHighPrice);
            gridView.Columns.Add(gvcOpenPrice);
            gridView.Columns.Add(gvcLowPrice);
            gridView.Columns.Add(gvcClosePrice);
            gridView.Columns.Add(gvcVolume);
            gridView.Columns.Add(gvcTurnover);
            gridView.Columns.Add(gvcPositon);
            MainListView.View = gridView;
        }

        private void ReadTick()
        {
            string strSrc = ConfigurationManager.AppSettings["futuretick"];
            string futureid = TbxInstrumentId.Text;
            string strFile = System.IO.Path.Combine(strSrc, Convert.ToDateTime(DpBeginDate.Text).ToString("yyyyMMdd"));
            strFile = System.IO.Path.Combine(strFile, futureid + ".tk");
            if (futureid.ToLower() != "index")
            {
                MqTickReader mqTickReader = new MqTickReader(strFile, futureid, EnumMarket.期货, "");
                List<Tick> ticklist = new List<Tick>();
                if (!mqTickReader.Read(ticklist, 0, int.MaxValue))
                {


                }
                MainListView.Items.Clear();
                double preVolume = 0;
                foreach (Tick tick in ticklist)
                {
                    ViewTick viewTick = new ViewTick(tick, preVolume);
                    MainListView.Items.Add(viewTick);
                    preVolume = viewTick.Volume;
                }
            }
        }
        private void ReadMin()
        {
            string strSrc = ConfigurationManager.AppSettings["futuremin"];
            string futureid = TbxInstrumentId.Text;
            DateTime beginDate = Convert.ToDateTime(DpBeginDate.Text);
            DateTime endDate = Convert.ToDateTime(DpEndDate.Text);

            BarSeries barSeries = new BarSeries();
            for (DateTime date = beginDate; date < endDate;)
            {
                string strFile = System.IO.Path.Combine(strSrc, date.ToString("yyyyMM"));
                strFile = System.IO.Path.Combine(strFile, futureid + ".min");
                MqMinReader mqMinReader = new MqMinReader(strFile);
                ;
                mqMinReader.ReadAll(barSeries);

                date = date.AddMonths(1);
            }


            MainListView.Items.Clear();
            foreach (Bar bar in barSeries)
            {
                ViewMin min = new ViewMin(bar);
                if (bar.TradingDate >= beginDate && bar.TradingDate <= endDate)
                    MainListView.Items.Add(min);
            }
        }
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            EDataType eDataType = (EDataType)CmbBox.SelectedItem;
            switch (eDataType)
            {
                case EDataType.Tick:
                    ReadTick();
                    break;
                case EDataType.Min:
                    ReadMin();
                    break;
                default:
                    ReadTick();
                    break;
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            EDataType eDataType = (EDataType)CmbBox.SelectedItem;
            StringBuilder sb = new StringBuilder();

            switch (eDataType)
            {
                case EDataType.Tick:
                    sb.AppendLine($@"TradingDay,TickNow,LastPrice,VolumeNow,BidPrice1,AskPrice1,BidVolume1,AskVolume1,HighPrice,OpenPrice,LowPrice,Change,Volume,Turnover");
                    break;
                case EDataType.Min:
                    sb.AppendLine($@"TradingDate,BeginTimeStr,EndTimeStr,High,Open,Low,Close,Volume,Turnover");
                    break;
                default:
                    break;
            }

            foreach (var data in MainListView.SelectedItems)
            {
                switch (eDataType)
                {
                    case EDataType.Tick:
                        ViewTick tick = (ViewTick)data;
                        sb.AppendLine($@"{tick.TradingDay},{tick.TickNow},{tick.LastPrice},{tick.VolumeNow},{tick.BidPrice1},{tick.AskPrice1},{tick.BidVolume1},{tick.AskVolume1},{tick.HighPrice},{tick.OpenPrice},{tick.LowPrice},{tick.Change},{tick.Volume},{tick.Turnover}");
                        break;
                    case EDataType.Min:
                        ViewMin min = (ViewMin)data;
                        sb.AppendLine($@"{min.TradingDate},{min.BeginTimeStr},{min.EndTimeStr},{min.High},{min.Open},{min.Low},{min.Close},{min.Volume},{min.Turnover}");
                        break;
                    default:
                        break;
                }
            }
            CsvProxy.AppendToCsv(sb.ToString(), TbxPath.Text, "data.csv");
        }
    }
}
