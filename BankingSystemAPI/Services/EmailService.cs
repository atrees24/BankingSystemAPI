using System.Net.Mail;
using System.Net;

namespace BankingSystemAPI.Services
{
    public class EmailService
    {
        public void SendOtpEmail(string toEmail, string otp)
        {
            var fromEmail = "atrees.ahmed5544@gmail.com";  // البريد الإلكتروني المرسل
            var fromPassword = "ywlwbejroezehmax";  // كلمة المرور للبريد الإلكتروني
            var smtpServer = "smtp.gmail.com";  // خادم البريد
            var smtpPort = 587;  // البورت المستخدم للبريد الصادر (عادةً 587)

            try
            {
                var smtp = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true  // تأكيد الاتصال المشفر
                };

                var mailMessage = new MailMessage(fromEmail, toEmail)
                {
                    Subject = "Your OTP for Banking Transaction",
                    Body = $"Your OTP is: {otp}. It is valid for 5 minutes."  // محتوى البريد
                };

                smtp.Send(mailMessage);  // إرسال البريد الإلكتروني
            }
            catch (Exception ex)
            {
                // التعامل مع الأخطاء في حالة حدوث مشكلة أثناء إرسال البريد الإلكتروني
                Console.WriteLine(ex.Message);
            }
        }
    }
}
