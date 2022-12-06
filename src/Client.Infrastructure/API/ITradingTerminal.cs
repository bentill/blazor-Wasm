using CT.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Infrastructure.API
{
    internal interface ITradingTerminal
    {
        bool Connected { get; }
        void ConnectAsync();
        void Disconnect();
        Task<IList<TradeRecord>> GetAllTradesAsync(CancellationToken token);
        Task<IList<TradeRecord>> GetCloseTradesAsync(DateTime dateTime, CancellationToken token);
    }
}
