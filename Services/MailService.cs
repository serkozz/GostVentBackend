using OneOf;
using Types.Classes;
using Types.Interfaces;
using Codes = System.Net.HttpStatusCode;

using MailKit.Net.Smtp;
using MimeKit;
using MailKit;
using Types.Enums;
using EF.Contexts;
using EF.Models;

namespace Services;

public class MailService
{
    private string? Source { get; set; }
    private string? SMTP_Username { get; set; }
    private string? SMTP_Password { get; set; }
    private string? SMTP_Host { get; set; }
    private int SMTP_Port { get; set; }

    private readonly SmtpClient _smtpClient;
    private readonly SQLiteContext _db;

    private ConfigurationManager _configManager;

    public MailService(ConfigurationManager configManager, SQLiteContext db)
    {
        _configManager = configManager;
        _db = db;

        Source = configManager.GetSection("SMTP:Mail:Source").Value;
        SMTP_Username = configManager.GetSection("SMTP:Mail:Username").Value;
        SMTP_Password = configManager.GetSection("SMTP:Mail:Password").Value;
        SMTP_Host = configManager.GetSection("SMTP:Mail:Host").Value;
        SMTP_Port = Convert.ToInt32(configManager.GetSection("SMTP:Mail:Port").Value);

        _smtpClient = new SmtpClient();
        _smtpClient.Connected += OnConnected;
        _smtpClient.Authenticated += OnAuthenticated;
        _smtpClient.Disconnected += OnDisconnected;

        (bool, bool) connectionResult = Connect();

        if (!connectionResult.Item1 || !connectionResult.Item2)
        {
            throw new Exception("Соединение с почтовым сервером не может быть установлено!!");
        }
    }

    public (bool, bool) Connect()
    {
        _smtpClient.Connect(SMTP_Host, SMTP_Port);
        _smtpClient.Authenticate(SMTP_Username, SMTP_Password);
        return (_smtpClient.IsConnected, _smtpClient.IsAuthenticated);
    }

    public bool Reconnect()
    {
        (bool, bool) connectionResult;

        for (var i = 0; i < 10; i++)
        {
            if (!_smtpClient.IsConnected)
            {
                connectionResult = Connect();
                continue;
            }
            return true;
        }
        return false;
    }

    public void OnConnected(object? sender, ConnectedEventArgs e)
    {
        // _logger.LogInformation($"Connected to Mail.Ru SMTP Server ---> Host: {e.Host}, Port: {e.Port}");
    }
    public void OnAuthenticated(object? sender, AuthenticatedEventArgs e)
    {
        // _logger.LogInformation($@"User {SMTP_Username} successfully authenticated ---> Message: {e.Message}");
    }
    public void OnDisconnected(object? sender, DisconnectedEventArgs e)
    {
        if (!Reconnect())
            throw new Exception("Невозможно подключиться к SMTP серверу Mail.ru");
        // _logger.LogInformation($@"Disconnected from Mail.Ru SMTP Server ---> Host: {e.Host}, Port: {e.Port}");
    }

    private Uri CreateConfirmationLink(string email, string fullUrl, Token token)
    {
        var confirmationUriString = fullUrl.ToString().Split('?', 2)[0] + $@"/confirm?email={email}&data={token.Value}";
        return new Uri(confirmationUriString);
    }

    public async Task<OneOf<Uri, ErrorInfo>> SendLink(string email, string fullUrl, Token token)
    {
        // if (!_smtpClient.IsConnected || !_smtpClient.IsAuthenticated)
        //     if (!Reconnect()) return new ErrorInfo(Codes.NotFound, "Невозможно подключиться к SMTP серверу");

        var confirmationUri = CreateConfirmationLink(email, fullUrl, token);
        var sendResult = await SendMessageAsync(email, "Удаление аккаунта ГостВент", confirmationUri.AbsoluteUri);
        return confirmationUri;
    }

    public async Task<OneOf<string, ErrorInfo>> SendMessageAsync(string destination, string subject, string content)
    {
        // if (!_smtpClient.IsConnected || !_smtpClient.IsAuthenticated)
        //     if (!Reconnect()) return new ErrorInfo(Codes.NotFound, "Невозможно подключиться к SMTP серверу");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("ГостВент", SMTP_Username));
        message.To.Add(new MailboxAddress("", destination));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = content
        };

        try
        {
            return await _smtpClient.SendAsync(message);
        }
        catch (Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, ex.Message);
        }
    }
}