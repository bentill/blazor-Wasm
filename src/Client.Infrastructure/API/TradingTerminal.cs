using Client.Infrastructure.Exceptions;
using CT;
using CT.Logging;
using CT.Trading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingAPI.MT4Server;
using TradeRecord = CT.Trading.TradeRecord;

namespace Client.Infrastructure.API
{

    [DebuggerDisplay("{ServerName} {Login}")]
    public class TradingTerminal : ILogSource, ITradingTerminal
    {
        private readonly string password;
        private readonly long accountId;

        public event DisconnectEventHandler OnDisconnect;


        public TradingTerminal(TradeAccount account)
        {
            this.accountId = account.ID;
            var reader = IniReader.Parse("", account.Configuration);
            Login = reader.ReadInt32("MT4", "Login", 0);
            password = reader.ReadString("MT4", "Password");
            Server = reader.ReadString("MT4", "Server");
            Port = reader.ReadInt32("MT4", "Port", 443);
            ServerName = reader.ReadString("Account", "Name");
        }

        public bool Connected => QuoteClient == null ? false : QuoteClient.Connected;

        public string LogSourceName => $"Terminal #{Login}";

        public string Server { get; }
        public int Port { get; }
        public int Login { get; }

        public string ServerName { get; }

        public QuoteClient QuoteClient { get; private set; }


        void ITradingTerminal.ConnectAsync()
        {
            this.LogInfo($"[{accountId}]({ServerName}) Connect to {Login}");
            QuoteClient = new QuoteClient((int)Login, password, Server, Port);
            try
            {
                QuoteClient.ConnectAsync();
                QuoteClient.ProcessEvents = ProcessEvents.SingleThread;
                QuoteClient.OnDisconnect += new DisconnectEventHandler(QC_OnDisconnect);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                if (ex.Message.Contains("QuoteConnector"))
                {
                    error = ex.InnerException.Message;
                }
                QuoteClient.Disconnect();

                throw new ConnectException($"[{accountId}]({ServerName}) {Login}: {error}");
            }
        }



        public void Disconnect()
        {
            QuoteClient?.Disconnect();
            UnSubscribe();
        }


        private void QC_OnDisconnect(object sender, DisconnectEventArgs args)
        {
            this.LogInfo($"<red>Disconnected<default> to \"{ServerName}\" S[{accountId}] S({Login})");
            OnDisconnect?.Invoke(this, args);
        }

        public void Reconnect()
        {
            UnSubscribe();
        }

        private void UnSubscribe()
        {
            if (QuoteClient != null)
            {
                QuoteClient.OnDisconnect -= QC_OnDisconnect;
                QuoteClient.OnDisconnect -= new DisconnectEventHandler(QC_OnDisconnect);
            }
        }

        public IList<TradeRecord> ParseOrderToTradeRecord(Order[] allorther)
        {
            IList<TradeRecord> tradeRecords = new List<TradeRecord>();
            foreach (var o in allorther)
            {
                tradeRecords.Add(new TradeRecord
                {
                    Ticket = o.Ticket,
                    Type = (TradeType)(int)o.Type,
                    Magic = o.MagicNumber,
                    Quantity = o.Lots,
                    OpenTime = o.OpenTime,
                    OpenPrice = o.OpenPrice,
                    SymbolName = o.Symbol,
                    Comment = o.Comment,
                    CloseTime = o.CloseTime,
                    Profit = o.Profit,
                    TakeProfit = o.TakeProfit,
                    Commission = o.Commission,
                    StopLoss = o.StopLoss,
                    Swap = o.Swap,
                    ClosePrice = o.ClosePrice,
                    PlacedReason = (TradeRecordReasonType)o.PlacedReason
                });
            }
            return tradeRecords;
        }

        public TradeRecord ParseOrderToTradeRecord(Order orther)
        {
            return new TradeRecord()
            {
                Ticket = orther.Ticket,
                State = (TradeState)orther.Ex.state,
                Type = (TradeType)(int)orther.Type,
                Magic = orther.MagicNumber,
                Quantity = orther.Lots,
                OpenTime = orther.OpenTime,
                OpenPrice = orther.OpenPrice,
                SymbolName = orther.Symbol,
                Comment = orther.Comment,
                CloseTime = orther.CloseTime,
                Profit = orther.Profit,
                TakeProfit = orther.TakeProfit,
                Commission = orther.Commission,
                StopLoss = orther.StopLoss,
                Swap = orther.Swap,
                ClosePrice = orther.ClosePrice,
                PlacedReason = (TradeRecordReasonType)orther.PlacedReason
            };
        }

        public QuoteEventArgs GetQuote(string symbol)
        {
            QuoteEventArgs quote = QuoteClient.GetQuote(symbol);
            int count = 10;
            while (quote == null && --count > 0)
            {
                Thread.Sleep(100);
                quote = QuoteClient.GetQuote(symbol);
            }
            if (quote == null)
            {
                return null;
            }
            else
            {
                return quote;
            }
        }
        public IList<TradeRecord> GetAllTrades()
        {
            if (QuoteClient.Connected == false)
            {
                throw new ConnectException($"[{accountId}]({ServerName}) {Login}: Disconnected");
            }
            var orders = QuoteClient.GetOpenedOrders();
            return ParseOrderToTradeRecord(orders);
        }

        public IList<TradeRecord> GetCloseTrade(DateTime dateTime)
        {
            var orderhistory = QuoteClient.DownloadOrderHistory(dateTime, QuoteClient.ServerTime);
            return ParseOrderToTradeRecord(orderhistory);
        }

        public async Task<IList<TradeRecord>> GetAllTradesAsync(CancellationToken token)
        {
            var orders = await Task.FromResult(GetAllTrades());
            return orders;
        }

        public async Task<IList<TradeRecord>> GetCloseTradesAsync(DateTime dateTime, CancellationToken token)
        {
            var orders = await Task.FromResult(GetCloseTrade(dateTime));
            return orders;
        }
    }
}

