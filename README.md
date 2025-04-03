# OpenTelemetryExample

This guide walks you through logging into New Relic, extracting configurations, cloning this repository, and setting up OpenTelemetry instrumentation.

## Prerequisites
- [New Relic account](https://newrelic.com/signup)


## Setup Instructions

### 1. Access New Relic Configuration
1. Log in to your [New Relic account](https://login.newrelic.com/login)
2. Navigate to **Your Account** > **(API Keys)** > **Create a key** 
3. select key type "License"
4. Copy your License key


### 2. Clone the Repository
set globalSettings.json to:

"OpenTelemetry": {
  "OTEL_EXPORTER_OTLP_ENDPOINT": "https://otlp.eu01.nr-data.net:4317",
  "OTEL_EXPORTER_OTLP_HEADERS": "api-key=(Paste your License key here)",
  "OTEL_SERVICE_NAME": "ServiceName",
  "OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT": 4095,
  "OTEL_EXPORTER_OTLP_COMPRESSION": "gzip",
  "OTEL_EXPORTER_OTLP_PROTOCOL": "http/protobuf",
  "OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE": "delta",
  "OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_ENABLE_GRPC_INSTRUMENTATION": "true"
}

