using System.Net.Mail;
using System.Net;

namespace BankingSystemAPI.Services
{
    public class EmailService
    {
        public void SendEmail(string toEmail, string otp)
        {
            var fromEmail = "stcbank96@gmail.com";
            var fromPassword = "skamzazvrlulwham";
            var smtpServer = "smtp.gmail.com";
            var smtpPort = 587;

            try
            {
                var smtp = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage(fromEmail, toEmail)
                {
                    Subject = "Your OTP for Banking Transaction",
                    Body = $"Your OTP is: {otp}. It is valid for 5 minutes."
                };

                smtp.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendEmail(string toEmail, string otp, int transactionId)
        {
            var fromEmail = "stcbank96@gmail.com";
            var fromPassword = "skamzazvrlulwham";
            var smtpServer = "smtp.gmail.com";
            var smtpPort = 587;

            try
            {
                var smtp = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage(fromEmail, toEmail)
                {
                    Subject = "Your OTP for Banking Transaction",
                    Body = $"Transaction ID: {transactionId}\n" +
                           $"OTP: {otp}\n" +
                           $"Please use this OTP to confirm your transaction."
                };

                smtp.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
