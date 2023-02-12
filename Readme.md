# MqttFileUploader

MqttFileUploader is a .NET Core command-line application that watches a local directory for changes and uploads new or modified files to an MQTT broker.

## Features

* Watch a local directory for new or modified files
* Upload files to an MQTT broker
* Compress files with GZip before uploading
* Support for disabling `Created`, `Changed`, and `Deleted` events
* Configuration settings can be specified in a TOML file
* SSL/TLS encryption support

## Getting Started

### Prerequisites

* .NET Core 7 or later
* An MQTT broker

### Installation

1. Clone the repository or download the source code
2. Build the project with `dotnet build`
3. Update the `config.toml` file with your MQTT broker settings and SSL/TLS options
4. Run the application with `dotnet run`

### Usage

MqttFileUploader is a command-line application. To start watching a directory for changes and uploading files to the MQTT broker, simply run the application with `dotnet run`. The application will continue running until you press `Ctrl+C` to stop it.

The `config.toml` file contains the following options:

* `directoryPath`: The local directory to watch for changes
* `brokerHostname`: The hostname of the MQTT broker to upload files to
* `brokerPort`: The port number of the MQTT broker to upload files to
* `brokerUsername`: The username to use to connect to the MQTT broker (optional)
* `brokerPassword`: The password to use to connect to the MQTT broker (optional)
* `topic`: The MQTT topic to publish files to
* `compress`: Whether to compress files with GZip before uploading
* `createdEventEnabled`: Whether to upload files that are created in the watched directory
* `changedEventEnabled`: Whether to upload files that are modified in the watched directory
* `deletedEventEnabled`: Whether to delete files on the MQTT broker when they are deleted from the watched directory
* `tlsEnabled`: Whether to use SSL/TLS encryption for the connection
* `tlsProtocol`: The SSL/TLS protocol version to use (e.g. `Tls12`)
* `tlsAllowUntrustedCertificates`: Whether to allow untrusted SSL/TLS certificates (useful for testing)

### Built With

* [.NET Core](https://dotnet.microsoft.com/) - The .NET framework used
* [MQTTnet](https://github.com/chkr1011/MQTTnet) - The MQTT library used
* [Tomlyn](https://github.com/xoofx/Tomlyn) - The TOML library used

### License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
