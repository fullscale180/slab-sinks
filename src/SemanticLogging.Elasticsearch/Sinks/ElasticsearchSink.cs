﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FullScale180.SemanticLogging.Properties;
using FullScale180.SemanticLogging.Utility;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace FullScale180.SemanticLogging.Sinks
{
    /// <summary>
    /// Sink that asynchronously writes entries to a Elasticsearch server.
    /// </summary>
    public class ElasticsearchSink : IObserver<EventEntry>, IDisposable
    {
        private const string BulkServiceOperationPath = "_bulk";

        private readonly BufferedEventPublisher<EventEntry> bufferedPublisher;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpClient client = new HttpClient();

        private readonly string index;
        private readonly string type;
        private readonly string instanceName;

        private readonly bool flattenPayload;

        private readonly Uri elasticsearchUrl;
        private readonly TimeSpan onCompletedTimeout;
        private readonly Dictionary<string, string> _jsonGlobalContextExtension;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElasticsearchSink"/> class with the specified connection string and table address.
        /// </summary>
        /// <param name="instanceName">The name of the instance originating the entries.</param>
        /// <param name="connectionString">The connection string for the storage account.</param>
        /// <param name="index">Index name prefix formatted as index-{0:yyyy.MM.DD}.</param>
        /// <param name="type">Elasticsearch entry type.</param>
        /// <param name="flattenPayload">Flatten the payload collection when serializing event entries</param>
        /// <param name="bufferInterval">The buffering interval to wait for events to accumulate before sending them to Elasticsearch.</param>
        /// <param name="bufferingCount">The buffering event entry count to wait before sending events to Elasticsearch </param>
        /// <param name="maxBufferSize">The maximum number of entries that can be buffered while it's sending to Windows Azure Storage before the sink starts dropping entries.</param>
        /// <param name="onCompletedTimeout">Defines a timeout interval for when flushing the entries after an <see cref="OnCompleted"/> call is received and before disposing the sink.
        /// This means that if the timeout period elapses, some event entries will be dropped and not sent to the store. Normally, calling <see cref="IDisposable.Dispose"/> on 
        /// the <see cref="System.Diagnostics.Tracing.EventListener"/> will block until all the entries are flushed or the interval elapses.
        /// If <see langword="null"/> is specified, then the call will block indefinitely until the flush operation finishes.</param>
        /// <param name="jsonGlobalContextExtension">A json encoded key/value set of global environment parameters to be included in each log entry</param>
        public ElasticsearchSink(string instanceName, string connectionString, string index, string type, bool? flattenPayload, TimeSpan bufferInterval,
            int bufferingCount, int maxBufferSize, TimeSpan onCompletedTimeout, string jsonGlobalContextExtension = null)
        {
            Guard.ArgumentNotNullOrEmpty(instanceName, "instanceName");
            Guard.ArgumentNotNullOrEmpty(connectionString, "connectionString");
            Guard.ArgumentNotNullOrEmpty(index, "index");
            Guard.ArgumentNotNullOrEmpty(type, "type");
            Guard.ArgumentIsValidTimeout(onCompletedTimeout, "onCompletedTimeout");
            Guard.ArgumentGreaterOrEqualThan(0, bufferingCount, "bufferingCount");

            if (Regex.IsMatch(index, "[\\\\/*?\",<>|\\sA-Z]"))
            {
                throw new ArgumentException(Resource.InvalidElasticsearchIndexNameError, "index");
            }

            this.onCompletedTimeout = onCompletedTimeout;

            this.instanceName = instanceName;
            this.flattenPayload = flattenPayload ?? true;
            this.elasticsearchUrl = new Uri(new Uri(connectionString), BulkServiceOperationPath);

            // AndMed: Logic to handle Basic auth of Elasticsearch
            string userInfo = Uri.UnescapeDataString(this.elasticsearchUrl.UserInfo);
            if (!string.IsNullOrEmpty(userInfo))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userInfo)));
            }

            this.index = index;
            this.type = type;
            var sinkId = string.Format(CultureInfo.InvariantCulture, "ElasticsearchSink ({0})", instanceName);
            bufferedPublisher = BufferedEventPublisher<EventEntry>.CreateAndStart(sinkId, PublishEventsAsync, bufferInterval,
                bufferingCount, maxBufferSize, cancellationTokenSource.Token);

            this._jsonGlobalContextExtension = !string.IsNullOrEmpty(jsonGlobalContextExtension)? JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonGlobalContextExtension): null;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="ElasticsearchSink"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            FlushSafe();
            Dispose();
        }

        /// <summary>
        /// Provides the sink with new data to write.
        /// </summary>
        /// <param name="value">The current entry to write to Windows Azure.</param>
        public void OnNext(EventEntry value)
        {
            if (value == null)
            {
                return;
            }

            bufferedPublisher.TryPost(value);
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            FlushSafe();
            Dispose();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ElasticsearchSink"/> class.
        /// </summary>
        ~ElasticsearchSink()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether or not the class is disposing.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed",
            MessageId = "cancellationTokenSource", Justification = "Token is canceled")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource.Cancel();
                bufferedPublisher.Dispose();
                client.Dispose();
            }
        }

        /// <summary>
        /// Causes the buffer to be written immediately.
        /// </summary>
        /// <returns>The Task that flushes the buffer.</returns>
        public Task FlushAsync()
        {
            return bufferedPublisher.FlushAsync();
        }

        internal async Task<int> PublishEventsAsync(IList<EventEntry> collection)
        {

            try
            {
                string logMessages;
                using (var serializer = new ElasticsearchEventEntrySerializer(this.index, this.type, this.instanceName, this.flattenPayload, this._jsonGlobalContextExtension))
                {
                    logMessages = serializer.Serialize(collection);
                }
                var content = new StringContent(logMessages);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync(this.elasticsearchUrl, content, cancellationTokenSource.Token).ConfigureAwait(false);

                // If there is an exception
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Check the response for 400 bad request
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var messagesDiscarded = collection.Count();

                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        string serverErrorMessage;

                        // Try to parse the exception message
                        try
                        {
                            var errorObject = JObject.Parse(errorContent);
                            serverErrorMessage = errorObject["error"].Value<string>();
                        }
                        catch (Exception)
                        {
                            // If for some reason we cannot extract the server error message log the entire response
                            serverErrorMessage = errorContent;
                        }

                        // We are unable to write the batch of event entries - Possible poison message
                        // I don't like discarding events but we cannot let a single malformed event prevent others from being written
                        // We might want to consider falling back to writing entries individually here
                        SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(string.Format("Elasticsearch sink unhandled exception {0} messages discarded with server error message {1}", messagesDiscarded, serverErrorMessage));

                        return messagesDiscarded;
                    }

                    // This will leave the messages in the buffer
                    return 0;
                }

                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseObject = JObject.Parse(responseString);

                var items = responseObject["items"] as JArray;

                // If the response return items collection
                if (items != null)
                {
                    // NOTE: This only works with Elasticsearch 1.0
                    // Alternatively we could query ES as part of initialization check results or fall back to trying <1.0 parsing
                    // We should also consider logging errors for individual entries
                    return items.Count(t => t["create"]["status"].Value<int>().Equals(201));

                    // Pre-1.0 Elasticsearch
                    // return items.Count(t => t["create"]["ok"].Value<bool>().Equals(true));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                // Although this is generally considered an anti-pattern this is not logged upstream and we have context
                SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(ex.ToString());
                throw;
            }
        }

        private void FlushSafe()
        {
            try
            {
                FlushAsync().Wait(onCompletedTimeout);
            }
            catch (AggregateException ex)
            {
                // Flush operation will already log errors. Never expose this exception to the observable.
                ex.Handle(e => e is FlushFailedException);
            }
        }
    }
}