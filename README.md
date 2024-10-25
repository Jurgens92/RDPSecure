# RDPSecure

RDPSecure is a C# application designed to enhance the security of Remote Desktop Protocol (RDP) connections by blocking brute force attacks. It monitors RDP connection attempts in real-time and takes action to prevent unauthorized access.

## Key Features

- **Real-time Monitoring**: Keeps an eye on all RDP connection attempts.
- **Brute Force Detection**: Identifies and blocks IP addresses that show patterns of brute force attacks.
- **Customizable Settings**: Allows users to define rules for blocking IP addresses.
- **Detailed Logging**: Records all connection attempts and actions taken for review.
- **Alert Notifications**: Sends alerts when suspicious activity is detected.

## How It Works

RDPSecure continuously monitors RDP connection attempts. When it detects multiple failed login attempts from the same IP address within a short period, it blocks that IP address for a specified duration to prevent further attempts.