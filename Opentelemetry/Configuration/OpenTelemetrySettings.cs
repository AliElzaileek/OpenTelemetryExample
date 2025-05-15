using Microsoft.Extensions.Configuration;


namespace CFX.OpenTelemetry.Configuration
{
    public class OpenTelemetrySettings
    {
        [ConfigurationKeyName("OTEL_EXPORTER_OTLP_ENDPOINT")]
        public string? OtelExporterOtlpEndpoint { get; set; }

        [ConfigurationKeyName("OTEL_EXPORTER_OTLP_HEADERS")]
        public string? OtelExporterOtlpHeaders { get; set; }

        [ConfigurationKeyName("OTEL_SERVICE_NAME")]
        public string? OtelServiceName { get; set; }

        [ConfigurationKeyName("OTEL_RESOURCE_ATTRIBUTES")]
        public string? OtelResourceAttributes { get; set; }

        [ConfigurationKeyName("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT")]
        public int? OtelAttributeValueLengthLimit { get; set; }

        [ConfigurationKeyName("OTEL_EXPORTER_OTLP_COMPRESSION")]
        public string? OtelExporterOtlpCompression { get; set; }

        [ConfigurationKeyName("OTEL_EXPORTER_OTLP_PROTOCOL")]
        public string? OtelExporterOtlpProtocol { get; set; }

        [ConfigurationKeyName("OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE")]
        public string? OtelExporterOtlpMetricsTemporalityPreference { get; set; }

        [ConfigurationKeyName("OTEL_SAMPLER")]
        public SamplerOptions? OtelSampler { get; set; }
    }

    public class SamplerOptions
    {
        [ConfigurationKeyName("OTEL_SAMPLER_NAME")]
        public string? OtelSamplerName { get; set; }

        [ConfigurationKeyName("OTEL_SAMPLER_RATIO")]
        public double? OtelSamplerRatio { get; set; }
    }
}
