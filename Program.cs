using System.IO.Compression;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using Nett;

namespace MqttFileUploader {
    class Program {

        private static async Task Main() {
            
            // Parse the TOML file to get the directory paths and MQTT broker settings
            var toml = Toml.ReadFile("config.toml");
            var directoryPaths = toml.Get<string[]>("directoryPaths");
            var includeSubdirectories = toml.Get<bool>("includeSubdirectories");
            var fileTypes = toml.Get<string>("fileTypes");
            var brokerHostname = toml.Get<string>("brokerHostname");
            var brokerPort = toml.Get<int>("brokerPort");
            var brokerUsername = toml.Get<string>("brokerUsername");
            var brokerPassword = toml.Get<string>("brokerPassword");
            var topic = toml.Get<string>("topic");
            var protocolVersion = toml.Get<int>("protocolVersion");
            var createdEventEnabled = toml.Get<bool>("createdEventEnabled");
            var changedEventEnabled = toml.Get<bool>("changedEventEnabled");
            var deletedEventEnabled = toml.Get<bool>("deletedEventEnabled");
           

            // Print configuration information to console
            PrintConfig();

            // Create an MQTT client and connect to the broker
            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();
            MqttClientOptions? options = null;
            
            if (toml.Get<bool>("sslEnabled")) {
                // Configure TLS options with certificate-based authentication
                var sslCertificatePath = toml.Get<string>("sslCertificatePath");
                if (!string.IsNullOrEmpty(sslCertificatePath)) {
                    var sslCertificate = new X509Certificate2(sslCertificatePath);
                    var tlsOptions = new MqttClientOptionsBuilderTlsParameters {
                        UseTls = true,
                        SslProtocol = SslProtocols.Tls12,
                        Certificates = new List<X509Certificate> {sslCertificate},
                        CertificateValidationHandler = (context) => {
                            // This is an example of a basic certificate validation handler. You should modify this to suit your needs.
                            if (context.SslPolicyErrors == SslPolicyErrors.None) {
                                return true;
                            }

                            Console.WriteLine($"Certificate validation error: {context.SslPolicyErrors}");
                            return false;
                        }
                    };

                    // Configure MQTT client options with TLS options
                    options = new MqttClientOptionsBuilder()
                        .WithTcpServer(brokerHostname, brokerPort)
                        .WithClientId(Guid.NewGuid().ToString())
                        .WithTls(tlsOptions)
                        .WithCredentials(brokerUsername, brokerPassword)
                        .WithProtocolVersion((MqttProtocolVersion)protocolVersion)
                        .Build();
                }
            }
            else {
                // Configure MQTT client options without TLS options
                options = new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerHostname, brokerPort)
                    .WithClientId(Guid.NewGuid().ToString())
                    .WithCredentials(brokerUsername, brokerPassword)
                    .WithProtocolVersion((MqttProtocolVersion)protocolVersion)
                    .Build();
            }

            await client.ConnectAsync(options);

            // Create a FileSystemWatcher for each directory path to monitor for changes
            var watchers = new List<FileSystemWatcher>();
            foreach (var directoryPath in directoryPaths) {
                var watcher = new FileSystemWatcher(directoryPath);
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.IncludeSubdirectories = includeSubdirectories;
                if (!string.IsNullOrWhiteSpace(fileTypes)) {
                    watcher.Filter = "*.*";
                }

                if (createdEventEnabled)
                    watcher.Created += (_, e) => {
                        // Upload the new file to the broker
                        var fileName = Send(e);
                        if (fileName != string.Empty) Console.WriteLine($"New file uploaded: {Path.GetFileName(fileName)}");
                    };
                if (changedEventEnabled)
                    watcher.Changed += (_, e) => {
                        // Upload the changed file to the broker
                        var fileName = Send(e);
                        if (fileName != string.Empty) Console.WriteLine($"File updated: {Path.GetFileName(fileName)}");
                    };
                if (deletedEventEnabled)
                    watcher.Deleted += (_, e) => {
                        // Delete the file from the broker
                        var fileName = Send(e);
                        if (fileName != string.Empty) Console.WriteLine($"File deleted: {Path.GetFileName(fileName)}");
                    };
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }

            // Wait for user input to exit the program
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            // Stop the FileSystemWatchers and disconnect from the broker
            foreach (var watcher in watchers) {
                watcher.EnableRaisingEvents = false;
            }

            await client.DisconnectAsync();

            string Send(FileSystemEventArgs e) {
                
                // Check if the file type is in the list of allowed types
                var extension = Path.GetExtension(e.FullPath).Remove(0, 1);
                if (!fileTypes.Contains("*.*") & !fileTypes.Contains(extension))
                    return string.Empty;
                
                var filePath = e.FullPath;
                var fileContents = e.ChangeType == WatcherChangeTypes.Deleted ? Array.Empty<byte>() : File.ReadAllBytes(filePath);
                if (toml.Get<bool>("compress")) {
                    using var compressedStream = new MemoryStream();
                    using var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress);
                    gzipStream.Write(fileContents, 0, fileContents.Length);
                    fileContents = compressedStream.ToArray();
                }

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(fileContents)
                    .Build();
                try {
                    client.PublishAsync(message).Wait();
                }
                catch (Exception exception) {
                    Console.WriteLine(exception);
                }

                return filePath;
            }

            void PrintConfig() {
                Console.WriteLine("Configuration:");
                Console.WriteLine($"  Directory paths:");
                foreach (var path in directoryPaths) Console.WriteLine($"    {path}");
                Console.WriteLine($"  Topic: {topic}");
                Console.WriteLine($"  File types: {(fileTypes == null ? "All files" : string.Join(", ", fileTypes))}");
                Console.WriteLine($"  Broker hostname: {brokerHostname}");
                Console.WriteLine($"  Broker port: {brokerPort}");
                Console.WriteLine($"  Broker username: {brokerUsername}");
                Console.WriteLine($"  Broker password: {(string.IsNullOrEmpty(brokerPassword) ? "Not set" : "****")}");
                Console.WriteLine($"  Created event enabled: {createdEventEnabled}");
                Console.WriteLine($"  Changed event enabled: {changedEventEnabled}");
                Console.WriteLine($"  Deleted event enabled: {deletedEventEnabled}");
            }
        }

    }
}