namespace PassMan.Server.Contexts
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Token
    {
        public Token(string value)
        {
            Value = value;
        }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual long Id { get; set; }

        public virtual string Value { get; set; }

        public virtual DateTime Timestamp { get; set; }

        public virtual DateTime Expire { get; set; }
    }
}