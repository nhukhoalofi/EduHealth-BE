namespace EduHealth.Helpers
{
    public static class OtpHelper
    {
        public static string GenerateOtp(int length = 6)
        {
            var random = new Random();
            var chars = new char[length];

            for (int i = 0; i < length; i++)
            {
                chars[i] = (char)('0' + random.Next(0, 10));
            }

            return new string(chars);
        }
    }
}