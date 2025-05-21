using System.Diagnostics.Metrics;
using System.Reflection;
using CFX.OpenTelemetry.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;

namespace CFX.OpenTelemetry
{
    public static class OpenTelemetryExtensions
    {
        public static string OpenTelemetrySettingsKey => "OpenTelemetry";

        public static ILoggingBuilder ConfigureOpenTelemetryLogging(this ILoggingBuilder logBuilder, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(logBuilder);
            ArgumentNullException.ThrowIfNull(configuration);

            logBuilder.ClearProviders();

            OpenTelemetrySettings otelSettings = GetOpenTelemetrySettings(configuration);

            logBuilder.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;

                ResourceBuilder resourceBuilder = CreateResourceBuilder(otelSettings);
                logging.SetResourceBuilder(resourceBuilder);

                //logging.AddConsoleExporter();

                if (!string.IsNullOrWhiteSpace(otelSettings.OtelExporterOtlpEndpoint))
                {
                    logging.AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(otelSettings.OtelExporterOtlpEndpoint);
                        opt.Headers = otelSettings.OtelExporterOtlpHeaders;
                        opt.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            });

            return logBuilder;
        }

        public static string GetApplicationNamespace(string applicationName)
        {
            string[] chunks = applicationName.Split(".");
            if (chunks.Length < 2)
            {
                return applicationName;
            }
            return string.Join(".", chunks.AsEnumerable().Skip(1));
        }

        public static IServiceCollection AddApplicationOpenTelemetry(this IServiceCollection services, IConfiguration configuration, Action<MeterProviderBuilder>? configureMeterProviderAction = null)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            OpenTelemetrySettings options = GetOpenTelemetrySettings(configuration);

            AssemblyName? assemblyName = Assembly.GetEntryAssembly()?.GetName();

            string? applicationName = assemblyName?.Name ?? options.OtelServiceName;
            string version = assemblyName?.Version?.ToString() ?? "unknown";
            string? applicationNamespace = GetApplicationNamespace(applicationName!);
            Sampler selectedSampler = GetSampler(configuration);

            services.Configure<OpenTelemetrySettings>(configuration.GetSection(OpenTelemetrySettingsKey));

            services.AddOpenTelemetry()
                    .ConfigureResource(resourceBuilder =>
                    {
                        resourceBuilder.AddService(serviceName: applicationName!,
                                                   serviceNamespace: applicationNamespace,
                                                   serviceVersion: version,
                                                   serviceInstanceId: Environment.MachineName);
                    })
                    .WithTracing(builder =>
                    {
                        builder.ConfigureResource((resourceBuilder) =>
                        {
                            resourceBuilder.AddService(serviceName: applicationName!,
                                                        serviceNamespace: applicationNamespace,
                                                        serviceVersion: version,
                                                        serviceInstanceId: Environment.MachineName);
                        })
                        .SetSampler(selectedSampler)
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddNpgsql()
                        .AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(options.OtelExporterOtlpEndpoint!);
                            opt.Headers = options.OtelExporterOtlpHeaders;
                            opt.Protocol = OtlpExportProtocol.Grpc;
                        });
                    })
                    .WithMetrics(builder =>
                    {
                        ResourceBuilder resourceBuilder = CreateResourceBuilder(options);
                        builder.SetResourceBuilder(resourceBuilder);

                        builder.ConfigureResource((resourceBuilder) =>
                                                  {
                                                      resourceBuilder.AddService(serviceName: applicationName!,
                                                                                 serviceNamespace: applicationNamespace,
                                                                                 serviceVersion: version,
                                                                                 serviceInstanceId: Environment.MachineName);
                                                  });

                        builder.AddRuntimeInstrumentation()
                               .AddAspNetCoreInstrumentation()
                               .AddHttpClientInstrumentation()
                               .AddNpgsqlInstrumentation();

                        configureMeterProviderAction?.Invoke(builder);

                        builder.AddOtlpExporter(opt =>
                                                {
                                                    opt.Endpoint = new Uri(options.OtelExporterOtlpEndpoint!);
                                                    opt.Headers = options.OtelExporterOtlpHeaders;
                                                    opt.Protocol = OtlpExportProtocol.Grpc;
                                                });

                        builder.AddView(instrument =>
                                        {
                                            return instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>) ? new Base2ExponentialBucketHistogramConfiguration() : null;
                                        });
                    })
                    .WithLogging(logging =>
                    {
                        logging.ConfigureResource((resourceBuilder) =>
                        {
                            resourceBuilder.AddService(serviceName: applicationName!,
                                                       serviceNamespace: applicationNamespace,
                                                       serviceVersion: version,
                                                       serviceInstanceId: Environment.MachineName);
                        })
                        .AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(options.OtelExporterOtlpEndpoint!);
                            opt.Headers = options.OtelExporterOtlpHeaders;
                            opt.Protocol = OtlpExportProtocol.Grpc;
                        });
                    });

            return services;
        }

        private static OpenTelemetrySettings GetOpenTelemetrySettings(IConfiguration configuration)
        {
            OpenTelemetrySettings otelSettings = configuration.GetSection(OpenTelemetrySettingsKey).Get<OpenTelemetrySettings>()
                ?? throw new InvalidOperationException("OpenTelemetry configuration is missing");

            if (string.IsNullOrEmpty(otelSettings.OtelServiceName))
                throw new InvalidOperationException("OpenTelemetry service name is not configured");

            if (string.IsNullOrEmpty(otelSettings.OtelExporterOtlpEndpoint))
                throw new InvalidOperationException("OpenTelemetry OTLP Endpoint is not configured");

            return otelSettings;
        }

        private static Sampler GetSampler(IConfiguration configuration)
        {
            OpenTelemetrySettings? otelSettings = configuration.GetSection(OpenTelemetrySettingsKey).Get<OpenTelemetrySettings>() ?? throw new InvalidOperationException("OpenTelemetry sampler configuration is missing");

            Sampler sampler = otelSettings.OtelSampler?.OtelSamplerName switch
            {
                "AlwaysOn" => new AlwaysOnSampler(),
                "AlwaysOff" => new AlwaysOffSampler(),
                "TraceIdRatioBased" => new TraceIdRatioBasedSampler(otelSettings.OtelSampler?.OtelSamplerRatio ?? 1.0),
                _ => new AlwaysOnSampler()
            };
            return sampler;
        }

        private static ResourceBuilder CreateResourceBuilder(OpenTelemetrySettings otelSettings)
        {
            ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: otelSettings.OtelServiceName!);

            if (!string.IsNullOrWhiteSpace(otelSettings.OtelResourceAttributes))
            {
                Dictionary<string, object> attributes = otelSettings.OtelResourceAttributes!
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split('=', 2))
                    .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    .ToDictionary(parts => parts[0], parts => (object)parts[1]);

                if (attributes.Any())
                {
                    resourceBuilder.AddAttributes(attributes);
                }
            }

            return resourceBuilder;
        }
    }
}
