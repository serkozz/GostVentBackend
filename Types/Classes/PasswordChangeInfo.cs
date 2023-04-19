namespace Types.Classes;
public class PasswordChangeInfo
{
    public string Email { get; set; }

    public string OldPassword { get; set; }

    public string NewPassword { get; set; }

    public string NewPasswordRepeated { get; set; }
}