using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;

namespace Types.Classes
{
    public static class Utility
    {
        public static string ToJSON(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Проводит проверку на силу пароля
        /// </summary>
        /// <param name="password">Пароль</param>
        /// <returns>Строки с непройдеными проверками (длина,содержание)</returns>
        public static string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();

            if (password.Length < 8)
                sb.AppendLine("Пароль должен быть не менее 8 символов в длину");
            if (!(Regex.IsMatch(password, "[a-z]", RegexOptions.IgnoreCase) || !Regex.IsMatch(password, "[а-я]", RegexOptions.IgnoreCase)))
                sb.AppendLine("Пароль должен содержать буквы");
            if (!Regex.IsMatch(password, "[0-9]"))
                sb.AppendLine("Пароль должен содержать числа");
            if (!Regex.IsMatch(password, "[!@#$%&*()_+=|<>?{}\\[\\]~-]"))
                sb.AppendLine("Пароль должен содержать специальные символы");
            return sb.ToString();
        }
    }
}