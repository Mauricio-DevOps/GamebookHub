using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace GamebookHub.Services;

public sealed class DemoUserStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public DemoUserStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "demo-users.json");
    }

    public async Task<StoredUser?> ValidateAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var normalized = email.Trim();
        var users = await ReadAsync();
        var user = users.SingleOrDefault(u => string.Equals(u.Email, normalized, StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            return null;
        }

        return string.Equals(user.PasswordHash, Hash(password), StringComparison.Ordinal)
            ? user
            : null;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var users = await ReadAsync();
        return users.Any(u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email inválido", nameof(email));
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Senha inválida", nameof(password));
        }

        await _lock.WaitAsync();
        try
        {
            var users = await ReadAsync();
            if (users.Any(u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Já existe um usuário com esse e-mail.");
            }

            users.Add(new StoredUser
            {
                Email = email.Trim(),
                PasswordHash = Hash(password),
                IsAdmin = false
            });

            await WriteAsync(users);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<StoredUser>> ReadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        var data = await JsonSerializer.DeserializeAsync<List<StoredUser>>(stream);
        return data ?? [];
    }

    private async Task WriteAsync(List<StoredUser> users)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, users, _jsonOptions);
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public sealed class StoredUser
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
