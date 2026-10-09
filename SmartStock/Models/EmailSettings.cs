namespace SmartStock.Models
{
    /// <summary>
    /// SMTP configuration bound from the "EmailSettings" section of appsettings.json.
    /// </summary>
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "SmartStock";
    }
}
