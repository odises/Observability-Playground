using Microsoft.Extensions.Logging;

using Prometheus;

using System;

namespace AlibabaTalk.Observability.Consumer
{
    public class MetricReporter
    {
        private readonly ILogger<MetricReporter> _logger;
        private readonly Counter _requestCounter;
        private readonly Histogram _responseTimeHistogram;

        public MetricReporter(ILogger<MetricReporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _requestCounter =
                Metrics.CreateCounter("total_requests", "The total number of requests serviced by this API.");

            _responseTimeHistogram = Metrics.CreateHistogram("request_duration_seconds",
                "The duration in seconds between the response to a request.", new HistogramConfiguration
                {
                    Buckets = Histogram.LinearBuckets(0.02, 3, 10),
                    LabelNames = new string[] { "IsSuccessful" }
                });
        }

        public void RegisterRequest()
        {
            _requestCounter.Inc();
        }

        public void RegisterResponseTime(bool isSuccessful, TimeSpan elapsed)
        {
            _responseTimeHistogram.Labels(isSuccessful.ToString()).Observe(elapsed.TotalSeconds);
        }
    }
}