using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Nexora.Infrastructure.Messaging
{
    public class KafkaMessageProducer : IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaMessageProducer> _logger;
        private readonly KafkaSettings _settings;
        private bool _disposed = false;

        public KafkaMessageProducer(IOptions<KafkaSettings> settings, ILogger<KafkaMessageProducer> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                ClientId = _settings.ClientId,
                Acks = Acks.All,
                EnableIdempotence = true,
                MaxInFlight = 1,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000,
                RequestTimeoutMs = 30000,
                MessageTimeoutMs = 300000,
                CompressionType = CompressionType.Snappy,
                BatchSize = 16384,
                LingerMs = 5,
                SecurityProtocol = _settings.SecurityProtocol,
                SaslMechanism = _settings.SaslMechanism,
                SaslUsername = _settings.SaslUsername,
                SaslPassword = _settings.SaslPassword
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Error}", e.Reason))
                .SetLogHandler((_, log) => _logger.LogDebug("Kafka producer log: {Message}", log.Message))
                .Build();
        }

        public async Task<bool> PublishSmsMessageAsync(SmsQueueMessage message)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var kafkaMessage = new Message<string, string>
                {
                    Key = message.MessageId,
                    Value = messageJson,
                    Headers = new Headers
                    {
                        { "MessageType", System.Text.Encoding.UTF8.GetBytes("SMS") },
                        { "TenantId", System.Text.Encoding.UTF8.GetBytes(message.TenantId.ToString()) },
                        { "Priority", System.Text.Encoding.UTF8.GetBytes(message.Priority.ToString()) },
                        { "CreatedAt", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                    }
                };

                var result = await _producer.ProduceAsync(_settings.SmsOutboundTopic, kafkaMessage);
                
                _logger.LogInformation("SMS message published to Kafka: MessageId={MessageId}, Partition={Partition}, Offset={Offset}", 
                    message.MessageId, result.Partition.Value, result.Offset.Value);

                return true;
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Failed to publish SMS message to Kafka: MessageId={MessageId}, Error={Error}", 
                    message.MessageId, ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing SMS message to Kafka: MessageId={MessageId}", 
                    message.MessageId);
                return false;
            }
        }

        public async Task<bool> PublishDeliveryReceiptAsync(DeliveryReceiptMessage receipt)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(receipt, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var kafkaMessage = new Message<string, string>
                {
                    Key = receipt.MessageId,
                    Value = messageJson,
                    Headers = new Headers
                    {
                        { "MessageType", System.Text.Encoding.UTF8.GetBytes("DeliveryReceipt") },
                        { "Status", System.Text.Encoding.UTF8.GetBytes(receipt.Status) },
                        { "ProviderId", System.Text.Encoding.UTF8.GetBytes(receipt.ProviderId.ToString()) },
                        { "ReceivedAt", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                    }
                };

                var result = await _producer.ProduceAsync(_settings.DeliveryReceiptTopic, kafkaMessage);
                
                _logger.LogInformation("Delivery receipt published to Kafka: MessageId={MessageId}, Status={Status}, Partition={Partition}, Offset={Offset}", 
                    receipt.MessageId, receipt.Status, result.Partition.Value, result.Offset.Value);

                return true;
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Failed to publish delivery receipt to Kafka: MessageId={MessageId}, Error={Error}", 
                    receipt.MessageId, ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing delivery receipt to Kafka: MessageId={MessageId}", 
                    receipt.MessageId);
                return false;
            }
        }

        public async Task<bool> PublishRetryMessageAsync(RetryQueueMessage retryMessage)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(retryMessage, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var kafkaMessage = new Message<string, string>
                {
                    Key = retryMessage.OriginalMessageId,
                    Value = messageJson,
                    Headers = new Headers
                    {
                        { "MessageType", System.Text.Encoding.UTF8.GetBytes("Retry") },
                        { "RetryAttempt", System.Text.Encoding.UTF8.GetBytes(retryMessage.RetryAttempt.ToString()) },
                        { "OriginalError", System.Text.Encoding.UTF8.GetBytes(retryMessage.LastError ?? "") },
                        { "ScheduledFor", System.Text.Encoding.UTF8.GetBytes(retryMessage.ScheduledRetryTime.ToString("O")) }
                    }
                };

                var result = await _producer.ProduceAsync(_settings.RetryTopic, kafkaMessage);
                
                _logger.LogInformation("Retry message published to Kafka: MessageId={MessageId}, Attempt={Attempt}, Partition={Partition}, Offset={Offset}", 
                    retryMessage.OriginalMessageId, retryMessage.RetryAttempt, result.Partition.Value, result.Offset.Value);

                return true;
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Failed to publish retry message to Kafka: MessageId={MessageId}, Error={Error}", 
                    retryMessage.OriginalMessageId, ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing retry message to Kafka: MessageId={MessageId}", 
                    retryMessage.OriginalMessageId);
                return false;
            }
        }

        public async Task<bool> PublishRateLimitedMessageAsync(RateLimitedMessage rateLimitedMessage)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(rateLimitedMessage, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var kafkaMessage = new Message<string, string>
                {
                    Key = rateLimitedMessage.MessageId,
                    Value = messageJson,
                    Headers = new Headers
                    {
                        { "MessageType", System.Text.Encoding.UTF8.GetBytes("RateLimited") },
                        { "ProviderId", System.Text.Encoding.UTF8.GetBytes(rateLimitedMessage.ProviderId.ToString()) },
                        { "ReleaseTime", System.Text.Encoding.UTF8.GetBytes(rateLimitedMessage.ReleaseTime.ToString("O")) },
                        { "RateLimitType", System.Text.Encoding.UTF8.GetBytes(rateLimitedMessage.RateLimitType) }
                    }
                };

                var result = await _producer.ProduceAsync(_settings.RateLimitTopic, kafkaMessage);
                
                _logger.LogInformation("Rate limited message published to Kafka: MessageId={MessageId}, ReleaseTime={ReleaseTime}, Partition={Partition}, Offset={Offset}", 
                    rateLimitedMessage.MessageId, rateLimitedMessage.ReleaseTime, result.Partition.Value, result.Offset.Value);

                return true;
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Failed to publish rate limited message to Kafka: MessageId={MessageId}, Error={Error}", 
                    rateLimitedMessage.MessageId, ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing rate limited message to Kafka: MessageId={MessageId}", 
                    rateLimitedMessage.MessageId);
                return false;
            }
        }

        public void Flush(TimeSpan timeout)
        {
            try
            {
                _producer.Flush(timeout);
                _logger.LogDebug("Kafka producer flushed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing Kafka producer");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _producer?.Flush(TimeSpan.FromSeconds(10));
                    _producer?.Dispose();
                    _logger.LogInformation("Kafka producer disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing Kafka producer");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }

    public class SmsQueueMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public int Priority { get; set; } = 5;
        public DateTime? ScheduledTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
        public string? CallbackUrl { get; set; }
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
    }

    public class DeliveryReceiptMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string ProviderMessageId { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal? Cost { get; set; }
        public string? Country { get; set; }
        public string? Operator { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new Dictionary<string, object>();
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }

    public class RetryQueueMessage
    {
        public string OriginalMessageId { get; set; } = string.Empty;
        public SmsQueueMessage OriginalMessage { get; set; } = new SmsQueueMessage();
        public int RetryAttempt { get; set; }
        public string? LastError { get; set; }
        public DateTime ScheduledRetryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string RetryReason { get; set; } = string.Empty;
        public Dictionary<string, object> RetryMetadata { get; set; } = new Dictionary<string, object>();
    }

    public class RateLimitedMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public SmsQueueMessage OriginalMessage { get; set; } = new SmsQueueMessage();
        public int ProviderId { get; set; }
        public DateTime ReleaseTime { get; set; }
        public string RateLimitType { get; set; } = string.Empty; // "provider", "tenant", "global"
        public int CurrentRate { get; set; }
        public int MaxRate { get; set; }
        public TimeSpan WindowDuration { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string ClientId { get; set; } = "nexora-sms-gateway";
        public string SmsOutboundTopic { get; set; } = "sms-outbound";
        public string DeliveryReceiptTopic { get; set; } = "sms-delivery-receipts";
        public string RetryTopic { get; set; } = "sms-retry";
        public string RateLimitTopic { get; set; } = "sms-rate-limited";
        public SecurityProtocol SecurityProtocol { get; set; } = SecurityProtocol.Plaintext;
        public SaslMechanism? SaslMechanism { get; set; }
        public string? SaslUsername { get; set; }
        public string? SaslPassword { get; set; }
        public string ConsumerGroupId { get; set; } = "nexora-sms-consumers";
        public int ConsumerSessionTimeoutMs { get; set; } = 30000;
        public int ConsumerMaxPollIntervalMs { get; set; } = 300000;
        public bool EnableAutoCommit { get; set; } = false;
        public int MaxConcurrentMessages { get; set; } = 10;
    }
}
