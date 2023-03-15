namespace PassMan.Model
{
    using System.ComponentModel;

    public enum DataSourceMode
    {
        [Description("Portable")]
        Portable,

        [Description("Appdata")]
        Appdata,

        [Description("Cloud")]
        Cloud,
    }
}