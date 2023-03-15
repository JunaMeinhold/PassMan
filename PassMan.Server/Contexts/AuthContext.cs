namespace PassMan.Server.Contexts
{
    using Microsoft.EntityFrameworkCore;

    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;
    using System.Text;

    public class AuthContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<Token> Tokens { get; set; }

        public AuthContext()
        {
            Database.Migrate();
            SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .EnableThreadSafetyChecks()
                .UseLazyLoadingProxies()
                .EnableDetailedErrors()
                .UseChangeTrackingProxies()
                .UseSqlite();
        }

        public bool TryGetUser(string username, [NotNullWhen(true)] out User? user)
        {
            user = Users.FirstOrDefault(x => x.Username == username);
            return user != null;
        }

        public bool TryAuthorize(string username, string password, [NotNullWhen(true)] out User? user)
        {
            user = null;
            if (!TryGetUser(username, out var uuser))
                return false;
            user = uuser;
            var hash = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(password)));
            return user.Password == hash;
        }

        public Token GenerateToken(User user)
        {
            byte[] keyBuffer = new byte[512];
            RandomNumberGenerator.Fill(keyBuffer);
            string key = Convert.ToBase64String(keyBuffer);
            Token token = new(key);
            user.Tokens.Add(token);
            Tokens.Add(token);
            SaveChanges();
            return token;
        }

        public bool CheckToken(string token)
        {
            var t = Tokens.FirstOrDefault(t => t.Value == token);
            if (t == null) return false;
            if (t.Expire < DateTime.Now) return false;
            return true;
        }
    }
}