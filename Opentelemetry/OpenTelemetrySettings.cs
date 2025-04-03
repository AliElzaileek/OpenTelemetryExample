using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFX.Opentelemetry
{
    internal class OpenTelemetrySettings
    {
        public string? OTEL_EXPORTER_OTLP_ENDPOINT { get; set; }
        public string? OTEL_EXPORTER_OTLP_HEADERS { get; set; }
        public string? OTEL_SERVICE_NAME { get; set; }
        public string? OTEL_RESOURCE_ATTRIBUTES { get; set; }
        public int? OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT { get; set; }
        public string? OTEL_EXPORTER_OTLP_COMPRESSION { get; set; }
        public string? OTEL_EXPORTER_OTLP_PROTOCOL { get; set; }
        public string? OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE { get; set; }
    }
}
