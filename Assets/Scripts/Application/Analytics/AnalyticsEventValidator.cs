namespace BlossomFlow.Application.Analytics
{
    public static class AnalyticsEventValidator
    {
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            for (var i = 0; i < name.Length; i++)
            {
                var character = name[i];
                if (!IsAsciiLetterOrDigit(character) && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAsciiLetterOrDigit(char character)
        {
            return character >= 'A' && character <= 'Z'
                || character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9';
        }
    }
}
