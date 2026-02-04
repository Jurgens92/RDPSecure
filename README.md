# RDPSecure

A lightweight, real-time Windows RDP brute force protection system that automatically detects and blocks suspicious login attempts. RDPSecure operates as both a background service and interactive dashboard to safeguard your Remote Desktop connections from unauthorized access.

## Features

### Core Protection
- **Real-time Event Monitoring** - Continuously monitors Windows Security Event Log for failed RDP login attempts (Event ID 4625)
- **Intelligent Brute Force Detection** - Configurable thresholds and time windows to identify attack patterns (default: 3 attempts within 5 minutes)
- **Automatic IP Blocking** - Instantly blocks malicious IPs via Windows Firewall integration
- **IPv4 & IPv6 Support** - Handles both address types seamlessly

### Advanced Security
- **Whitelist Management** - Protect trusted IPs from being blocked, even with failed attempts
- **Blacklist Management** - Immediately ban known malicious IPs on first detection
- **CIDR Subnet Support** - Block entire IP ranges if needed
- **IP Geolocation** - Automatically identifies geographical location of banned IPs
- **Smart Ban Duration** - Configurable durations (1 hour for private IPs, 30 days for public by default)

### Management & Monitoring
- **Dashboard GUI** - User-friendly interface for viewing stats, banned IPs, and settings
- **Persistent Ban Storage** - Bans survive application restarts via SQLite database
- **Automatic Cleanup** - Expired bans are automatically removed every 60 seconds
- **Audit Logging** - Complete security event trail for compliance and forensics
- **Auto-Updates** - Checks GitHub Releases for updates (hourly)

### Deployment
- **Dual Mode Operation** - Run as Windows Service (background) or GUI application (interactive)
- **Single Instance Protection** - Prevents multiple running instances
- **Centralized Configuration** - JSON-based settings management with hot-reload capability

## Requirements

- Windows 7 or later
- .NET Framework (version specified in project)
- Administrator privileges (required for firewall and event log access)
- Windows Event Log access enabled

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/Jurgens92/RDPSecure.git
   cd RDPSecure
   ```

2. Build the solution:
   ```bash
   dotnet build RDPSecure.sln -c Release
   ```

3. Run as GUI (interactive):
   ```bash
   RDPSecure.exe
   ```

   Or install as Windows Service:
   ```bash
   RDPSecure.exe --install
   ```

## Usage

### GUI Mode
Launch the application normally to open the dashboard. From here you can:
- View real-time login attempt statistics
- See the list of currently banned IPs with locations and ban duration
- Access whitelist/blacklist management
- Configure application settings
- Manually ban IP addresses

### Service Mode
Install as a Windows Service for automated background protection:
```bash
# Install the service
RDPSecure.exe --install

# Start the service
net start RDPSecure

# Stop the service
net stop RDPSecure

# Remove the service
RDPSecure.exe --uninstall
```

## Configuration

Access settings through the GUI or modify the configuration file directly:

- **Failed Attempt Threshold** - Number of failed attempts before blocking (default: 3)
- **Detection Window** - Time period to count attempts (default: 5 minutes)
- **Private IP Ban Duration** - How long to ban private IPs (default: 1 hour)
- **Public IP Ban Duration** - How long to ban public IPs (default: 30 days)
- **Whitelist/Blacklist Rules** - Per-entry enable/disable with notes for documentation

## How It Works

```
1. Monitor Security Event Log
   └─ Capture failed RDP login events (Event ID 4625)

2. Process Login Attempts
   ├─ Extract source IP address
   ├─ Check whitelist → Allow if present
   ├─ Check blacklist → Ban immediately if present
   └─ Track attempt with timestamp

3. Detect Brute Force
   ├─ Count attempts within time window
   └─ If threshold exceeded → Ban IP

4. Block Malicious IP
   ├─ Add to in-memory blocked list
   ├─ Create Windows Firewall rule (netsh)
   ├─ Persist to database
   ├─ Trigger geolocation lookup
   └─ Update dashboard

5. Automatic Cleanup
   ├─ Check for expired bans (every 60 seconds)
   ├─ Remove from firewall
   └─ Update database
```

## Security Notes

- **Firewall-level blocking** ensures IPs are blocked at the network kernel level
- **Private IP detection** distinguishes between internal and external threats
- **IP validation** prevents spoofing and malformed addresses
- **Event-driven architecture** enables real-time response without polling delays
- **Audit trail** provides full logging for security investigations and compliance

## Project Structure

- `RDPSecure/` - Main application directory
  - `Program.cs` - Entry point with service vs GUI mode selection
  - `MainForm.cs` - Dashboard UI
  - `Services/` - Core monitoring and firewall logic
  - `Models/` - Data structures (LoginAttempt, BanInfo)
  - `Data/` - Database persistence layer
  - `Logging/` - Security event logging
  - `Utility/` - IP validation, subnet utilities, update management
- `installer/` - Application installer files

## License

[Add your license information here]

## Contributing

Contributions are welcome. Please ensure code follows the existing patterns and add appropriate logging for new features.

## Support

For issues, feature requests, or questions, please open an issue on GitHub.