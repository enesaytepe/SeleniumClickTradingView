using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Timers;

namespace SeleniumClickTradingView
{
    class Program
    {
        //Bitcoin
        private static string tradingViewUrl = "https://tr.tradingview.com/symbols/BTCUSDT/technicals/";
        private static string coinMarketUrl = "https://coinmarketcap.com/currencies/bitcoin/";

        //Ethereum
        //private static string tradingViewUrl = "https://tr.tradingview.com/symbols/ETHUSDT/technicals/";
        //private static string coinMarketUrl = "https://coinmarketcap.com/currencies/ethereum/";

        private static System.Timers.Timer indicatorTimer;
        private static DateTime previousTime;
        //private static StreamWriter sr = null;
        //private static StreamWriter capitalLog = null;
        private static float totalCapital = 100000;
        private static float totalCoin = 0;
        private static float price = 0;

        private static IWebElement WaitUntilEl(IWebDriver dr, By by, int maxAttempt = 10)
        {
            int xTimesAttempted = 0;

            //Todo: Boş tanımlama gerekli.
            var el = dr.FindElements(by);

            do
            {
                xTimesAttempted++;
                el = dr.FindElements(by);
                Thread.Sleep(100);
            } while (xTimesAttempted < maxAttempt && el.Count < 1);
          
            return el[0];

        }

        private static string WaitUntilSignalText(IWebElement el)
        {
            var status = new List<string> { "al", "sat", "nötr" , "güçlü al", "güçlü sat" };
            string elementText;
            bool contains;

            do
            {
                elementText = el.GetAttribute("innerText").Trim().ToLower();
                contains = status.Contains(elementText);
                Thread.Sleep(100);

            } while (!contains);
          
            return elementText;
        }

        static void Main(string[] args)
        {
            //sr = new StreamWriter(@"C:\Users\enes_\Desktop\Interval.txt");
            //sr.AutoFlush = true;

            //capitalLog = new StreamWriter(@"C:\Users\enes_\Desktop\CapitalLog.txt");
            //capitalLog.AutoFlush = true;

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            var options = new ChromeOptions();
            options.AddArgument("--window-position=-32000,-32000");

            //TradingViewDriver
            IWebDriver tradingViewDriver = new ChromeDriver(); //service, options
            IJavaScriptExecutor tradingViewJs = (IJavaScriptExecutor)tradingViewDriver;
            tradingViewDriver.Navigate().GoToUrl(tradingViewUrl);
            WebDriverWait tradingViewWait = new WebDriverWait(tradingViewDriver, TimeSpan.FromSeconds(1));
            tradingViewWait.Until(wd => tradingViewJs.ExecuteScript("return document.readyState").ToString() == "complete");

            //TradingViewDriver
            IWebDriver coinMarketDriver = new ChromeDriver();
            IJavaScriptExecutor coinMarketJs = (IJavaScriptExecutor)coinMarketDriver;
            coinMarketDriver.Navigate().GoToUrl(coinMarketUrl);
            WebDriverWait coinMarketWait = new WebDriverWait(coinMarketDriver, TimeSpan.FromSeconds(1));
            coinMarketWait.Until(wd => coinMarketJs.ExecuteScript("return document.readyState").ToString() == "complete");

            //TradingView Timer
            indicatorTimer = new System.Timers.Timer(5000);
            indicatorTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e, tradingViewDriver, coinMarketDriver);
            indicatorTimer.AutoReset = true;
           
            indicatorTimer.Enabled = true;

            Console.WriteLine("Press the Enter key to exit the program... ");
            Console.ReadLine();

            Console.WriteLine("Terminating the application...");
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e, IWebDriver tradingViewDriver, IWebDriver coinMarketDriver)
        {
            //sr.WriteLine(String.Format("Elapsed event at {0:HH':'mm':'ss.ffffff}", e.SignalTime));
            previousTime = e.SignalTime;

            IWebElement btn1M = WaitUntilEl(tradingViewDriver, By.Id("1m"));
            btn1M.Click();

            //string osilator = WaitUntilSignalText(WaitUntilEl(tradingViewDriver, By.CssSelector("div[class*=\"speedometersContainer\"] > div:nth-child(1) > span[class*=\"speedometerSignal\"]")));
            string ozet = WaitUntilSignalText(WaitUntilEl(tradingViewDriver, By.CssSelector("div[class*=\"speedometersContainer\"] > div:nth-child(2) > span[class*=\"speedometerSignal\"]")));
            //string hareketliOrtalama = WaitUntilSignalText(WaitUntilEl(tradingViewDriver, By.CssSelector("div[class*=\"speedometersContainer\"] > div:nth-child(3) > span[class*=\"speedometerSignal\"]")));
            //string sonuc = "--Osilatörler: " + osilator + " Özet: " + ozet + " Hareketli Ortalama: " + hareketliOrtalama + "--";

            //priceValue
            IWebElement priceEl = WaitUntilEl(coinMarketDriver, By.CssSelector(".priceValue"));

            string priceStr = priceEl.GetAttribute("innerText").Trim().ToLower().Replace(",", "").Replace(".", ",");
            priceStr = priceStr.Substring(1, priceStr.Length - 1);

            price = float.Parse(priceStr);

            if (price > 0)
            {
                if ((ozet == "al" || ozet == "güçlü al") && totalCapital > price)
                {
                    totalCoin = totalCapital / price;
                    totalCapital = 0;
                }
                else if ((ozet == "sat" || ozet == "güçlü sat") && totalCoin > 0)
                {
                    totalCapital = totalCoin * price;
                    totalCoin = 0;
                }
            }

            string sonuc = "Sinyal:" + ozet + " Fiyat: " + price.ToString("0.0000") + " Sermaye: " + totalCapital.ToString("0.0000") + " Coin: " + totalCoin.ToString("0.0000") + " Cüzdan Toplamı:" + (totalCapital + (totalCoin * price)).ToString("0.0000");
            Console.WriteLine(sonuc);
            //sr.WriteLine(sonuc);

            //if (nEventsFired == 20)
            //{
            //    Console.WriteLine("No more events will fire...");
            //    aTimer.Enabled = false;
            //}
        }
    }
}
