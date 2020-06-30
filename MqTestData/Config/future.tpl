<?xml version="1.0" encoding="utf-8"?>
<template>
  <Exchanges>
    <Exchange id="CFFEX" name="中国金融交易所">
      <TradingTime>9:30,11:30,13:00,15:00</TradingTime>
      <OpenTime>9:30</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
    <Exchange id="SHFE" name="上海期货交易所">
      <TradingTime>9:00,10:15,10:30,11:30,13:30,15:00</TradingTime>
      <OpenTime>9:00</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
    <Exchange id="DCE" name="大连商品交易所">
      <TradingTime>9:00,10:15,10:30,11:30,13:30,15:00</TradingTime>
      <OpenTime>9:00</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
    <Exchange id="CZCE" name="郑州商品交易所">
      <TradingTime>9:00,10:15,10:30,11:30,13:30,15:00</TradingTime>
      <OpenTime>9:00</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
	<Exchange id="SH" name="上海证券交易所">
	  <TradingTime>9:30,11:30,13:00,15:00</TradingTime>
      <OpenTime>9:30</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
    <Exchange id="SZ" name="深圳证券交易所">
      <TradingTime>9:30,11:30,13:00,15:00</TradingTime>
      <OpenTime>9:30</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
	 <Exchange id="SGX" name="新加坡交易所">
      <TradingTime>9:00,11:30,13:00,16:00</TradingTime>
      <OpenTime>9:30</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
	 <Exchange id="SGE" name="上海黄金交易所">
      <TradingTime>9:00,10:15,10:30,11:30,13:30,15:00</TradingTime>
      <OpenTime>9:30</OpenTime>
      <CloseTime>15:00</CloseTime>
    </Exchange>
  </Exchanges>

  <Products>
    <Product id="cu" name="沪铜" exchange="SHFE" tradingtime="21:00,01:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="al" name="沪铝" exchange="SHFE" tradingtime="21:00,01:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="zn" name="沪锌" exchange="SHFE" tradingtime="21:00,01:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="pb" name="沪铅" exchange="SHFE" tradingtime="21:00,01:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="ag" name="白银" exchange="SHFE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="au" name="黄金" exchange="SHFE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="rb" name="螺纹钢" exchange="SHFE" tradingtime="21:00,01:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="wr" name="线材" exchange="SHFE"/>
    <Product id="fu" name="燃料油" exchange="SHFE"/>
    <Product id="ru" name="天然橡胶" exchange="SHFE" tradingtime="21:00,23:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="bu" name="石油沥青" exchange="SHFE" tradingtime="21:00,01:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
	<Product id="hc" name="热轧卷板" exchange="SHFE" tradingtime="21:00,01:00,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="c" name="玉米" exchange="DCE"/>
    <Product id="a" name="豆一" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="b" name="豆二" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="m" name="豆粕" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="y" name="豆油" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="p" name="棕榈油" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="l" name="聚乙烯" exchange="DCE"/>
    <Product id="v" name="PVC" exchange="DCE"/>
    <Product id="j" name="焦炭" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="jm" name="焦煤" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="i" name="铁矿石" exchange="DCE" tradingtime="21:00,02:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
	<Product id="jd" name="鲜鸡蛋" exchange="DCE"/>
	<Product id="fb" name="纤维板" exchange="DCE"/>
	<Product id="bb" name="胶合板" exchange="DCE"/>
	<Product id="pp" name="聚丙烯" exchange="DCE"/>
    <Product id="WS" name="强麦" exchange="CZCE"/>
    <Product id="WH" name="硬麦" exchange="CZCE"/>
    <Product id="PM" name="普麦" exchange="CZCE"/>
    <Product id="CF" name="棉花" exchange="CZCE" tradingtime="21:00,23:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="SR" name="白糖" exchange="CZCE" tradingtime="21:00,23:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="TA" name="PTA" exchange="CZCE" tradingtime="21:00,23:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="RO" name="菜籽油" exchange="CZCE"/>
    <Product id="ER" name="早籼稻" exchange="CZCE"/>
    <Product id="ME" name="甲醇" exchange="CZCE" tradingtime="21:00,23:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
    <Product id="FG" name="玻璃" exchange="CZCE"/>
    <Product id="RS" name="油菜籽" exchange="CZCE"/>
    <Product id="RM" name="油菜粕" exchange="CZCE" tradingtime="21:00,23:30,9:00,10:15,10:30,11:30,13:30,15:00"/>
	<Product id="OI" name="新菜籽油" exchange="CZCE"/>
    <Product id="TC" name="动力煤" exchange="CZCE"/>
	<Product id="LR" name="晚籼稻" exchange="CZCE"/>
	<Product id="SF" name="硅铁" exchange="CZCE"/>
	<Product id="SM" name="锰硅" exchange="CZCE"/>
    <Product id="IF" name="沪深300指数" exchange="CFFEX"/>
    <Product id="TF" name="国债期货" exchange="CFFEX"/>
	<Product id="IH" name="上证50指数" exchange="CFFEX"/>
	<Product id="IC" name="中证500指数" exchange="CFFEX"/>
  </Products>
  <Futures>
    <Future id="9999" name="主力合约" pid="default"/>
    <Future id="IF9999" name="主力合约" pid="IF"/>
    <Future id="IF9998" name="当月连续" pid="IF"/>
    <Future id="IF9997" name="下月连续" pid="IF"/>
    <Future id="IF9996" name="下季连续" pid="IF"/>
    <Future id="IF9995" name="隔季连续" pid="IF"/>
    <Future id="IF9990" name="股指指数" pid="IF"/>
	<Future id="IF9991" name="次主力合约" pid="IF"/>
    <Future id="9990" name="指数合约" pid="default"/>
	<Future id="9991" name="次主力合约" pid="default"/>
  </Futures>
</template>
