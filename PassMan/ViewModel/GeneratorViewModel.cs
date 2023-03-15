namespace PassMan.ViewModel
{
    using PassMan.Core.Commands;
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class GeneratorViewModel : ViewModelBase
    {
        internal static readonly char[] alphabeticChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        internal static readonly char[] numericChars = "1234567890".ToCharArray();
        internal static readonly char[] symbolsChars = "!§$%&/()=?\\*+-/#~'-_.,;:<>|^°\"".ToCharArray();
        private string password = string.Empty;
        private bool alphabetic;
        private bool numeric;
        private bool symbols;
        private int length = 16;

        public GeneratorViewModel()
        {
            GeneratePasswordCommand = new(GeneratePassword, () => alphabetic || numeric || symbols && length > 0);
        }

        public RelayCommand GeneratePasswordCommand { get; }

        public string Password
        {
            get => password;
            set => SetAndNotifyPropertyChanged(ref password, value);
        }

        public bool Alphabetic
        {
            get => alphabetic;
            set => SetAndNotifyPropertyChanged(ref alphabetic, value);
        }

        public bool Numeric
        {
            get => numeric;
            set => SetAndNotifyPropertyChanged(ref numeric, value);
        }

        public bool Symbols
        {
            get => symbols;
            set => SetAndNotifyPropertyChanged(ref symbols, value);
        }

        public int Length
        {
            get => length;
            set => SetAndNotifyPropertyChanged(ref length, value);
        }

        public void GeneratePassword()
        {
            Password = GetUniqueKey(Length, Alphabetic, Numeric, Symbols);
        }

        public static string GetUniqueKey(int size, bool alphabetic, bool numeric, bool symbols)
        {
            int maxIndex = 0;
            if (alphabetic)
                maxIndex += alphabeticChars.Length;
            if (numeric)
                maxIndex += numericChars.Length;
            if (symbols)
                maxIndex += symbolsChars.Length;

            byte[] data = new byte[4 * size];

            RandomNumberGenerator.Fill(data);

            StringBuilder result = new(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % maxIndex;
                if (idx < alphabeticChars.Length)
                {
                    result.Append(alphabeticChars[idx]);
                    continue;
                }
                idx -= alphabeticChars.Length;

                if (idx < numericChars.Length)
                {
                    result.Append(numericChars[idx]);
                    continue;
                }
                idx -= numericChars.Length;

                if (idx < symbolsChars.Length)
                {
                    result.Append(symbolsChars[idx]);
                    continue;
                }
            }

            return result.ToString();
        }
    }
}