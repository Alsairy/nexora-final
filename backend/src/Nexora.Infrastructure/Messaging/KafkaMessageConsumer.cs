using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Nexora.Core.Interfaces;

namespace Nexora.Infrastructure.Messaging
{
    public class KafkaMessageConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaMessageConsumer> _logger;
        private readonly KafkaSettings _settings;
        private readonly SemaphoreSlim _semaphore;
        private IConsumer<string, string>? _consumer;

        public KafkaMessageConsumer(
            IServiceProvider serviceProvider,
            IOptions<KafkaSettings> settings,
            ILogger<KafkaMessageConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _logger = logger;
            _semaphore = new SemaphoreSlim(_settings.MaxConcurrentMessages, _settings.MaxConcurrentMessages);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.ConsumerGroupId,
                ClientId = _settings.ClientId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = _settings.EnableAutoCommit,
                SessionTimeoutMs = _settings.ConsumerSessionTimeoutMs,
                MaxPollIntervalMs = _settings.ConsumerMaxPollIntervalMs,
                SecurityProtocol = _settings.SecurityProtocol,
                SaslMechanism = _settings.SaslMechanism,
                SaslUsername = _settings.SaslUsername,
                SaslPassword = _settings.SaslPassword,
                EnablePartitionEof = false,
                AllowAutoCreateTopics = false
            };

            _consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
                .SetLogHandler((_, log) => _logger.LogDebug("Kafka consumer log: {Message}", log.Message))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation("Partitions assigned: {Partitions}", 
                        string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogInformation("Partitions revoked: {Partitions}", 
                        string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .Build();

            var topics = new[]
            {
                _settings.SmsOutboundTopic,
                _settings.DeliveryReceiptTopic,
                _settings.RetryTopic,
                _settings.RateLimitTopic
            };

            _consumer.Subscribe(topics);
            _logger.LogInformation("Kafka consumer started, subscribed to topics: {Topics}", string.Join(", ", topics));

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        if (consumeResult?.Message != null)
                        {
                            await _semaphore.WaitAsync(stoppingToken);
                            
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await ProcessMessageAsync(consumeResult);
                                    
                                    if (!_settings.EnableAutoCommit)
                                    {
                                        _consumer.Commit(consumeResult);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing message from topic {Topic}, partition {Partition}, offset {Offset}", 
                                        consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
                                }
                                finally
                                {
                                    _semaphore.Release();
                                }
                            }, stoppingToken);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error: {Error}", ex.Error.Reason);
                        
                        if (ex.Error.IsFatal)
                        {
                            _logger.LogCritical("Fatal Kafka error, stopping consumer");
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Kafka consumer operation cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error in Kafka consumer");
                        await Task.Delay(5000, stoppingToken); // Wait before retrying
                    }
                }
            }
            finally
            {
                try
                {
                    _consumer?.Close();
                    _logger.LogInformation("Kafka consumer closed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing Kafka consumer");
                }
            }
        }

        private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult)
        {
            var messageType = GetMessageType(consumeResult.Message.Headers);
            var topic = consumeResult.Topic;

            _logger.LogDebug("Processing message: Topic={Topic}, MessageType={MessageType}, Key={Key}, Partition={Partition}, Offset={Offset}",
                topic, messageType, consumeResult.Message.Key, consumeResult.Partition.Value, consumeResult.Offset.Value);

            try
            {
                switch (topic)
                {
                    case var t when t == _settings.SmsOutboundTopic:
                        await ProcessSmsOutboundMessage(consumeResult.Message.Value);
                        break;
                    
                    case var t when t == _settings.DeliveryReceiptTopic:
                        await ProcessDeliveryReceiptMessage(consumeResult.Message.Value);
                        break;
                    
                    case var t when t == _settings.RetryTopic:
                        await ProcessRetryMessage(consumeResult.Message.Value);
                        break;
                    
                    case var t when t == _settings.RateLimitTopic:
                        await ProcessRateLimitedMessage(consumeResult.Message.Value);
                        break;
                    
                    default:
                        _logger.LogWarning("Unknown topic: {Topic}", topic);
                        break;
                }

                _logger.LogDebug("Message processed successfully: Topic={Topic}, Key={Key}", topic, consumeResult.Message.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from topic {Topic}: {Error}", topic, ex.Message);
                
                await HandleFailedMessage(consumeResult, ex);
            }
        }

        private async Task ProcessSmsOutboundMessage(string messageJson)
        {
            var smsMessage = JsonSerializer.Deserialize<SmsQueueMessage>(messageJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (smsMessage == null)
            {
                _logger.LogError("Failed to deserialize SMS outbound message");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

            try
            {
                if (smsMessage.ScheduledTime.HasValue && smsMessage.ScheduledTime.Value > DateTime.UtcNow)
                {
                    _logger.LogInformation("Message {MessageId} is scheduled for {ScheduledTime}, re-queuing", 
                        smsMessage.MessageId, smsMessage.ScheduledTime.Value);
                    
                    await RequeueScheduledMessage(smsMessage);
                    return;
                }

                if (await IsRateLimited(smsMessage))
                {
                    _logger.LogInformation("Message {MessageId} is rate limited, moving to rate limit queue", smsMessage.MessageId);
                    await MoveToRateLimitQueue(smsMessage);
                    return;
                }

                var request = MapToSendSmsRequest(smsMessage);
                var response = await smsService.SendSmsAsync(request);

                if (!response.Success)
                {
                    _logger.LogWarning("SMS sending failed for message {MessageId}: {Error}", 
                        smsMessage.MessageId, response.ErrorMessage);
                    
                    if (ShouldRetry(response.ErrorCode, smsMessage.RetryCount))
                    {
                        await MoveToRetryQueue(smsMessage, response.ErrorMessage);
                    }
                    else
                    {
                        _logger.LogError("SMS message {MessageId} failed permanently after {RetryCount} retries", 
                            smsMessage.MessageId, smsMessage.RetryCount);
                    }
                }
                else
                {
                    _logger.LogInformation("SMS message {MessageId} sent successfully", smsMessage.MessageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SMS outbound message {MessageId}", smsMessage.MessageId);
                
                if (ShouldRetry("PROCESSING_ERROR", smsMessage.RetryCount))
                {
                    await MoveToRetryQueue(smsMessage, ex.Message);
                }
            }
        }

        private async Task ProcessDeliveryReceiptMessage(string messageJson)
        {
            var receipt = JsonSerializer.Deserialize<DeliveryReceiptMessage>(messageJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (receipt == null)
            {
                _logger.LogError("Failed to deserialize delivery receipt message");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

            try
            {
                await smsService.ProcessDeliveryReceiptAsync(
                    receipt.ProviderMessageId,
                    receipt.Status,
                    receipt.DeliveredAt,
                    receipt.ErrorCode,
                    receipt.ErrorMessage);

                _logger.LogInformation("Delivery receipt processed for message {MessageId}, status: {Status}", 
                    receipt.MessageId, receipt.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delivery receipt for message {MessageId}", receipt.MessageId);
            }
        }

        private async Task ProcessRetryMessage(string messageJson)
        {
            var retryMessage = JsonSerializer.Deserialize<RetryQueueMessage>(messageJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (retryMessage == null)
            {
                _logger.LogError("Failed to deserialize retry message");
                return;
            }

            if (retryMessage.ScheduledRetryTime > DateTime.UtcNow)
            {
                _logger.LogDebug("Retry message {MessageId} not ready yet, scheduled for {ScheduledTime}", 
                    retryMessage.OriginalMessageId, retryMessage.ScheduledRetryTime);
                
                await RequeueRetryMessage(retryMessage);
                return;
            }

            _logger.LogInformation("Processing retry message {MessageId}, attempt {Attempt}", 
                retryMessage.OriginalMessageId, retryMessage.RetryAttempt);

            retryMessage.OriginalMessage.RetryCount = retryMessage.RetryAttempt;
            
            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaMessageProducer>();
            
            await producer.PublishSmsMessageAsync(retryMessage.OriginalMessage);
        }

        private async Task ProcessRateLimitedMessage(string messageJson)
        {
            var rateLimitedMessage = JsonSerializer.Deserialize<RateLimitedMessage>(messageJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (rateLimitedMessage == null)
            {
                _logger.LogError("Failed to deserialize rate limited message");
                return;
            }

            if (rateLimitedMessage.ReleaseTime > DateTime.UtcNow)
            {
                _logger.LogDebug("Rate limited message {MessageId} not ready yet, release time: {ReleaseTime}", 
                    rateLimitedMessage.MessageId, rateLimitedMessage.ReleaseTime);
                
                await RequeueRateLimitedMessage(rateLimitedMessage);
                return;
            }

            _logger.LogInformation("Processing rate limited message {MessageId}, releasing from {RateLimitType} rate limit", 
                rateLimitedMessage.MessageId, rateLimitedMessage.RateLimitType);

            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaMessageProducer>();
            
            await producer.PublishSmsMessageAsync(rateLimitedMessage.OriginalMessage);
        }

        private string GetMessageType(Headers headers)
        {
            if (headers.TryGetLastBytes("MessageType", out var messageTypeBytes))
            {
                return System.Text.Encoding.UTF8.GetString(messageTypeBytes);
            }
            return "Unknown";
        }

        private async Task HandleFailedMessage(ConsumeResult<string, string> consumeResult, Exception exception)
        {
            _logger.LogError("Moving message to dead letter queue: Topic={Topic}, Key={Key}, Error={Error}", 
                consumeResult.Topic, consumeResult.Message.Key, exception.Message);
            
        }

        private async Task<bool> IsRateLimited(SmsQueueMessage message)
        {
            return false;
        }

        private bool ShouldRetry(string errorCode, int currentRetryCount)
        {
            const int maxRetries = 3;
            
            if (currentRetryCount >= maxRetries)
                return false;

            var retryableErrors = new[]
            {
                "TIMEOUT",
                "NETWORK_ERROR",
                "PROVIDER_UNAVAILABLE",
                "RATE_LIMITED",
                "PROCESSING_ERROR"
            };

            return retryableErrors.Contains(errorCode);
        }

        private async Task MoveToRetryQueue(SmsQueueMessage originalMessage, string error)
        {
            var retryMessage = new RetryQueueMessage
            {
                OriginalMessageId = originalMessage.MessageId,
                OriginalMessage = originalMessage,
                RetryAttempt = originalMessage.RetryCount + 1,
                LastError = error,
                ScheduledRetryTime = CalculateRetryTime(originalMessage.RetryCount + 1),
                RetryReason = error
            };

            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaMessageProducer>();
            
            await producer.PublishRetryMessageAsync(retryMessage);
        }

        private async Task MoveToRateLimitQueue(SmsQueueMessage message)
        {
            var rateLimitedMessage = new RateLimitedMessage
            {
                MessageId = message.MessageId,
                OriginalMessage = message,
                ProviderId = message.ProviderId,
                ReleaseTime = DateTime.UtcNow.AddMinutes(1), // Simple 1-minute delay
                RateLimitType = "provider"
            };

            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaMessageProducer>();
            
            await producer.PublishRateLimitedMessageAsync(rateLimitedMessage);
        }

        private DateTime CalculateRetryTime(int retryAttempt)
        {
            var delayMinutes = Math.Pow(2, retryAttempt);
            return DateTime.UtcNow.AddMinutes(delayMinutes);
        }

        private async Task RequeueScheduledMessage(SmsQueueMessage message)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            
            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaMessageProducer>();
            
            await producer.PublishSmsMessageAsync(message);
        }

        private async Task RequeueRetryMessage(RetryQueueMessage retryMessage)
        {
            var delay = retryMessage.ScheduledRetryTime - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }

            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaMessageProducer>();
            
            await producer.PublishRetryMessageAsync(retryMessage);
        }

        private async Task RequeueRateLimitedMessage(RateLimitedMessage rateLimitedMessage)
        {
            var delay = rateLimitedMessage.ReleaseTime - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }

            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<KafkaMessageProducer>();
            
            await producer.PublishRateLimitedMessageAsync(rateLimitedMessage);
        }

        private Core.DTOs.SendSmsRequest MapToSendSmsRequest(SmsQueueMessage queueMessage)
        {
            return new Core.DTOs.SendSmsRequest
            {
                ToNumber = queueMessage.ToNumber,
                Message = queueMessage.Message,
                SenderId = queueMessage.SenderId,
                MessageType = queueMessage.MessageType,
                ProviderId = queueMessage.ProviderId > 0 ? queueMessage.ProviderId : null,
                ScheduledTime = queueMessage.ScheduledTime,
                CallbackUrl = queueMessage.CallbackUrl,
                CustomData = queueMessage.Metadata
            };
        }

        public override void Dispose()
        {
            try
            {
                _consumer?.Close();
                _consumer?.Dispose();
                _semaphore?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Kafka consumer");
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
