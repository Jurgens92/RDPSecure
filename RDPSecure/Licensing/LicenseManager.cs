using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RDPSecure.Logging;

namespace RDPSecure.Licensing
{
    public class LicenseKey
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int ValidityDays { get; set; }
        public DateTime? ActivationDate { get; set; }
    }

    public class LicenseManager
    {
        private static readonly byte[] EncryptionKey;
        private static readonly byte[] IV;
        private readonly string _licensePath;
        private readonly string _trialPath;
        private readonly ISecurityLogger _logger;

        public string TrialPath => _trialPath;

        static LicenseManager()
        {
            // Initialize encryption key (must match KeyGenerator)
            using (var deriveBytes = new Rfc2898DeriveBytes("R#DPS3cur3L1c3ns3K3y!2024",
                Encoding.UTF8.GetBytes("S@ltV@lu3"), 10000))
            {
                EncryptionKey = deriveBytes.GetBytes(32); // 256 bits
                IV = deriveBytes.GetBytes(16); // 128 bits
            }
        }

        public LicenseManager(ISecurityLogger logger)
        {
            _logger = logger;
            _licensePath = Path.Combine(AppConfig.AppDataPath, "license.dat");
            _trialPath = Path.Combine(AppConfig.AppDataPath, "trial.dat");
        }

        public bool IsTrialValid()
        {
            try
            {
                if (!File.Exists(_trialPath))
                {
                    // First run - start trial
                    var trialData = new { StartDate = DateTime.UtcNow };
                    Directory.CreateDirectory(Path.GetDirectoryName(_trialPath)!);
                    File.WriteAllText(_trialPath, JsonConvert.SerializeObject(trialData));
                    _logger.LogInformation("Trial period started");
                    return true;
                }

                var trialJson = File.ReadAllText(_trialPath);
                var trial = JsonConvert.DeserializeObject<dynamic>(trialJson)!;
                var startDate = DateTime.Parse(trial.StartDate.ToString());
                var daysRemaining = 30 - (DateTime.UtcNow - startDate).TotalDays;

                if (daysRemaining > 0)
                {
                    _logger.LogInformation($"Trial valid. {Math.Ceiling(daysRemaining)} days remaining");
                    return true;
                }

                _logger.LogInformation("Trial period expired");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking trial validity", ex);
                return false;
            }
        }

        public (bool IsValid, DateTime? ExpiryDate) ValidateLicense()
        {
            try
            {
                if (!File.Exists(_licensePath))
                {
                    return (IsTrialValid(), null);
                }

                var encryptedLicense = File.ReadAllText(_licensePath);
                var licenseKey = DecryptLicense(encryptedLicense);

                if (licenseKey.ActivationDate == null)
                {
                    licenseKey.ActivationDate = DateTime.UtcNow;
                    SaveLicense(licenseKey);
                    _logger.LogInformation("License activated for first use");
                }

                var expiryDate = licenseKey.ActivationDate.Value.AddDays(licenseKey.ValidityDays);
                var isValid = DateTime.UtcNow <= expiryDate;

                _logger.LogInformation(isValid
                    ? $"Valid license found. Expires on {expiryDate:d}"
                    : $"License expired on {expiryDate:d}");

                return (isValid, expiryDate);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error validating license", ex);
                return (IsTrialValid(), null);
            }
        }

        public bool ActivateLicense(string encryptedLicense)
        {
            try
            {
                var licenseKey = DecryptLicense(encryptedLicense);

                // Validate the license key
                if (licenseKey.ValidityDays <= 0)
                {
                    _logger.LogError("Invalid license validity period");
                    return false;
                }

                if (licenseKey.CreatedDate > DateTime.UtcNow)
                {
                    _logger.LogError("License has invalid creation date");
                    return false;
                }

                SaveLicense(licenseKey);
                _logger.LogInformation($"License activated successfully. Valid for {licenseKey.ValidityDays} days");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error activating license", ex);
                return false;
            }
        }

        private void SaveLicense(LicenseKey license)
        {
            try
            {
                var encryptedLicense = EncryptLicense(license);
                Directory.CreateDirectory(Path.GetDirectoryName(_licensePath)!);
                File.WriteAllText(_licensePath, encryptedLicense);
                _logger.LogInformation("License saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving license", ex);
                throw;
            }
        }

        private static string EncryptLicense(LicenseKey license)
        {
            var json = JsonConvert.SerializeObject(license);
            var plainBytes = Encoding.UTF8.GetBytes(json);

            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new BinaryWriter(cs))
                    {
                        sw.Write(plainBytes);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private static LicenseKey DecryptLicense(string encryptedLicense)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedLicense);

            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream(encryptedBytes))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    var json = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<LicenseKey>(json)!;
                }
            }
        }

        public void RemoveLicense()
        {
            try
            {
                if (File.Exists(_licensePath))
                {
                    File.Delete(_licensePath);
                    _logger.LogInformation("License removed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error removing license", ex);
                throw;
            }
        }
    }
}