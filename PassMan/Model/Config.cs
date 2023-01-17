namespace PassMan.Model
{
    using PassMan.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Config
    {
        public ProviderType ProviderType { get; set; } = ProviderType.Local;
    }
}