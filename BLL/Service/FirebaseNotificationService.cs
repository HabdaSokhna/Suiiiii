using FirebaseAdmin.Messaging;

namespace BLL.Service
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string token, string title, string body);
    }

    public class FirebaseNotificationService : INotificationService
    {
        public async Task SendNotificationAsync(string token, string title, string body)
        {
            if (string.IsNullOrEmpty(token)) return;

            var message = new Message()
            {
                Token = token,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                }
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine("Successfully sent message: " + response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending FCM notification: " + ex.Message);
            }
        }
    }
}