namespace Application.Extensions
{
    public class StringExtensions
    {
        public static string GetPaidLastValue(string paidvalue)
        {
            return paidvalue.Replace("TITL", "")
                .Replace("i", "")
                .TrimStart('0');
        }
    }
}