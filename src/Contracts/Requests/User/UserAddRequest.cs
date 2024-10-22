using System.Text.RegularExpressions;

namespace Contracts.Requests.User;

public class UserAddRequest
{
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public enum PasswordScore
{
    Blank = -1,
    VeryVeryWeak = 0,
    VeryWeak = 1,
    Weak = 2,
    Medium = 3,
    Strong = 4,
    VeryStrong = 5
}

public class PasswordAdvisor
{
    public static PasswordScore CheckStrength(string password)
    {
        int score = 0;

        if (password.Length < 1)
            return PasswordScore.Blank;
        if (password.Length < 4)
            return PasswordScore.VeryVeryWeak;
        if (password.Length >= 8)
            score++;
        if (password.Length >= 12)
            score++;
        if (Regex.Match(password, @"/\d+/", RegexOptions.ECMAScript).Success)
            score++;
        if (Regex.Match(password, @"/[a-z]/", RegexOptions.ECMAScript).Success &&
          Regex.Match(password, @"/[A-Z]/", RegexOptions.ECMAScript).Success)
            score++;
        if (Regex.Match(password, @"/.[!,@,#,$,%,^,&,*,?,_,~,-,£,(,)]/", RegexOptions.ECMAScript).Success)
            score++;

        return (PasswordScore)score;
    }
}
