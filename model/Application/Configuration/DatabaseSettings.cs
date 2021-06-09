namespace Application.Configuration
{
    public class DatabaseSettings
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string ConnectionString => $"User ID={User};Password={Password};Host={Host};Port=5432;Database={Name};Pooling=true;";
    }
}