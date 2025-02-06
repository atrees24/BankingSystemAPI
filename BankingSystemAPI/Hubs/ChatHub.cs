using BankingSystemAPI.Data;
using BankingSystemAPI.Models.RealTimeChat;
using Microsoft.AspNetCore.SignalR;

namespace BankingSystemAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly RealTimeChatContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(RealTimeChatContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendMessage(string userName, string message)
        {
            await Clients.Others.SendAsync("ReceiveMessage", userName, message);

            var msg = new Message()
            {
                UserName = userName,
                Text = message
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();
        }
    }
}
