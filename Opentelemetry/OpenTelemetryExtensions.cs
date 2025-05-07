using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Reflection;



namespace CFX.Opentelemetry
{
    public static class OpenTelemetryExtensions
    {
        public static ILoggingBuilder LoggingConfiguration(this ILoggingBuilder logBuilder, IConfiguration configuration)
        {
            if (logBuilder == null) throw new ArgumentNullException(nameof(logBuilder));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
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

                if (!string.IsNullOrWhiteSpace(otelSettings.OTEL_EXPORTER_OTLP_ENDPOINT))
                {
                    logging.AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(otelSettings.OTEL_EXPORTER_OTLP_ENDPOINT);
                        opt.Headers = otelSettings.OTEL_EXPORTER_OTLP_HEADERS;
                        opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
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
            return string.Join(".", chunks.AsEnumerable().Skip(1).SkipLast(1));
        }

        public static IServiceCollection AddApplicationTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            OpenTelemetrySettings options = GetOpenTelemetrySettings(configuration);
            string? applicationName = options.OTEL_SERVICE_NAME;
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            string? applicationNamespace = GetApplicationNamespace(applicationName!);

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
                            //.AddConsoleExporter()
                            .AddHttpClientInstrumentation()
                            .AddAspNetCoreInstrumentation()
                            .AddOtlpExporter(opt =>
                            {
                                opt.Endpoint = new Uri(options.OTEL_EXPORTER_OTLP_ENDPOINT!);
                                opt.Headers = options.OTEL_EXPORTER_OTLP_HEADERS;
                                opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
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
                        })

                               //.AddConsoleExporter()
                               .AddRuntimeInstrumentation()
                               .AddAspNetCoreInstrumentation()
                               .AddHttpClientInstrumentation()
                                //.AddProcessInstrumentation()
                                //.AddSqlClientInstrumentation()
                                .AddOtlpExporter(opt =>
                                {
                                    opt.Endpoint = new Uri(options.OTEL_EXPORTER_OTLP_ENDPOINT!);
                                    opt.Headers = options.OTEL_EXPORTER_OTLP_HEADERS;
                                    opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                                })
                               .AddView(instrument =>
                               {
                                   return instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>)
                                       ? new Base2ExponentialBucketHistogramConfiguration()
                                       : null;
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
                        .AddConsoleExporter()
                        .AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(options.OTEL_EXPORTER_OTLP_ENDPOINT!);
                            opt.Headers = options.OTEL_EXPORTER_OTLP_HEADERS;
                            opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        });
                    });
            //services.AddSingleton(typeof(OpenTelemetryExtensions));
            return services;
        }

        private static OpenTelemetrySettings GetOpenTelemetrySettings(IConfiguration configuration)
        {
            OpenTelemetrySettings otelSettings = configuration.GetSection("OpenTelemetry").Get<OpenTelemetrySettings>()
                ?? throw new InvalidOperationException("OpenTelemetry configuration is missing");

            if (string.IsNullOrEmpty(otelSettings.OTEL_SERVICE_NAME))
                throw new InvalidOperationException("OpenTelemetry service name is not configured");

            if (string.IsNullOrEmpty(otelSettings.OTEL_EXPORTER_OTLP_ENDPOINT))
                throw new InvalidOperationException("OpenTelemetry OTLP Endpoint is not configured");

            return otelSettings;
        }

        private static ResourceBuilder CreateResourceBuilder(OpenTelemetrySettings otelSettings)
        {
            ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: otelSettings.OTEL_SERVICE_NAME!);

            if (!string.IsNullOrWhiteSpace(otelSettings.OTEL_RESOURCE_ATTRIBUTES))
            {
                Dictionary<string, object> attributes = otelSettings.OTEL_RESOURCE_ATTRIBUTES!
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
