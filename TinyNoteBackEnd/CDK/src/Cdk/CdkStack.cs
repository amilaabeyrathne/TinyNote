using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.ServiceDiscovery;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.RDS;
using Constructs;

namespace Cdk
{
    public class CdkStack : Stack
    {
        public IVpc Vpc { get; }
        public ISecurityGroup AlbSecurityGroup { get; }
        public ISecurityGroup EcsTasksSecurityGroup { get; }
        public ISecurityGroup RdsSecurityGroup { get; }
        public IDatabaseInstance Database { get; }
        public DockerImageAsset ApiImage { get; }
        public DockerImageAsset FrontendImage { get; }

        internal CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // VPC with 2 AZs, public and private subnets (1 NAT Gateway to reduce cost)
            Vpc = new Vpc(this, "TinyNoteVpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 1,
            });

            // Security group for ALB - allow HTTP/HTTPS from internet
            AlbSecurityGroup = new SecurityGroup(this, "AlbSecurityGroup", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Allow HTTP/HTTPS traffic to ALB",
                AllowAllOutbound = true,
            });
            AlbSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(80), "Allow HTTP");
            AlbSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(443), "Allow HTTPS");

            // Security group for ECS tasks - allow traffic only from ALB
            EcsTasksSecurityGroup = new SecurityGroup(this, "EcsTasksSecurityGroup", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Allow traffic from ALB to ECS tasks",
                AllowAllOutbound = true,
            });
            EcsTasksSecurityGroup.AddIngressRule(Peer.SecurityGroupId(AlbSecurityGroup.SecurityGroupId), Port.Tcp(80), "Allow HTTP from ALB (frontend)");
            EcsTasksSecurityGroup.AddIngressRule(Peer.SecurityGroupId(AlbSecurityGroup.SecurityGroupId), Port.Tcp(8080), "Allow port 8080 from ALB (API)");

            // Security group for RDS - allow PostgreSQL from ECS API tasks only
            RdsSecurityGroup = new SecurityGroup(this, "RdsSecurityGroup", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Allow PostgreSQL access from API tasks",
                AllowAllOutbound = false,
            });
            RdsSecurityGroup.AddIngressRule(Peer.SecurityGroupId(EcsTasksSecurityGroup.SecurityGroupId), Port.Tcp(5432), "Allow PostgreSQL from API tasks");

            // RDS PostgreSQL 16 in private subnets
            Database = new DatabaseInstance(this, "TinyNoteDatabase", new DatabaseInstanceProps
            {
                Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps { Version = PostgresEngineVersion.VER_16 }),
                InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO),
                Vpc = Vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                SecurityGroups = new[] { RdsSecurityGroup },
                DatabaseName = "tinynote",
                Credentials = Credentials.FromPassword("postgres", SecretValue.UnsafePlainText("postgres")),
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            // ECR image assets - use assembly location for reliable paths when CDK spawns the app
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var cdkRoot = Path.GetFullPath(Path.Combine(assemblyDir!, "..", "..", "..", "..", ".."));
            var backendRoot = Path.GetFullPath(Path.Combine(cdkRoot, ".."));
            var frontendDir = Path.GetFullPath(Path.Combine(backendRoot, "..", "TinyNoteFrontEnd", "TinyNote"));

            ApiImage = new DockerImageAsset(this, "ApiImage", new DockerImageAssetProps
            {
                Directory = backendRoot,
                File = "TinyNote.Api/Dockerfile",
                Platform = Platform_.LINUX_AMD64,  // Use strongly-typed CDK Platform enum, not string
            });

            FrontendImage = new DockerImageAsset(this, "FrontendImage", new DockerImageAssetProps
            {
                Directory = frontendDir,
            });

            // Connection string from Database resource (port 5432 = PostgreSQL standard)
            var connectionString = $"Host={Database.InstanceEndpoint.Hostname};Port=5432;Database=tinynote;Username=postgres;Password=postgres";

            // ECS Cluster with a private DNS namespace (tinynote.local).
            // Creates a Route 53 private hosted zone visible only inside the VPC.
            // The collector registers an A record: collector.tinynote.local → task IP.
            var cluster = new Cluster(this, "TinyNoteCluster", new ClusterProps
            {
                Vpc = Vpc,
                ClusterName = "tinynote-cluster",
                DefaultCloudMapNamespace = new CloudMapNamespaceOptions
                {
                    Name = "tinynote.local",
                    Type = NamespaceType.DNS_PRIVATE,
                    Vpc = Vpc,
                },
            });

            // -----------------------------------------------------------------------
            // ADOT Collector Service (central, no sidecar per API task)
            // API tasks resolve collector.tinynote.local via Route 53 private DNS → task IP
            // Direct HTTP connection, no proxy required
            // -----------------------------------------------------------------------

            // SG for the collector tasks - accepts OTLP directly from API task SG
            var collectorTaskSg = new SecurityGroup(this, "CollectorTaskSg", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Allow OTLP from API tasks to collector",
                AllowAllOutbound = true,
            });
            collectorTaskSg.AddIngressRule(
                Peer.SecurityGroupId(EcsTasksSecurityGroup.SecurityGroupId),
                Port.Tcp(4318),
                "Allow OTLP HTTP from API tasks");

            // CloudWatch Log Group for the collector container stdout/stderr
            var collectorLogGroup = new LogGroup(this, "CollectorLogGroup", new LogGroupProps
            {
                LogGroupName = "/ecs/tinynote/collector",
                Retention = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            // CloudWatch Log Group where the awsemf exporter writes EMF metric entries
            // (auto-extracted by CloudWatch into the TinyNote metrics namespace)
            var metricsLogGroup = new LogGroup(this, "MetricsLogGroup", new LogGroupProps
            {
                LogGroupName = "/aws/otel/tinynote-metrics",
                Retention = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            // ADOT Collector task definition
            var collectorTaskDef = new FargateTaskDefinition(this, "CollectorTaskDef", new FargateTaskDefinitionProps
            {
                Cpu = 256,
                MemoryLimitMiB = 512,
                RuntimePlatform = new RuntimePlatform
                {
                    CpuArchitecture = CpuArchitecture.X86_64,
                    OperatingSystemFamily = OperatingSystemFamily.LINUX,
                },
            });

            // Allow the collector task role to write EMF entries to CloudWatch Logs
            collectorTaskDef.TaskRole.AddManagedPolicy(
                ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy"));

            // Allow the execution role to pull the ADOT image from ECR Public (public.ecr.aws).
            // Fargate requires ecr-public:GetAuthorizationToken even for anonymous pulls.
            collectorTaskDef.ObtainExecutionRole().AddManagedPolicy(
                ManagedPolicy.FromAwsManagedPolicyName("AmazonElasticContainerRegistryPublicReadOnly"));

            // ADOT Collector config:
            //   - Receives OTLP HTTP on 4318
            //   - Exports metrics via awsemf (EMF JSON → CloudWatch Metrics namespace "TinyNote")
            //   - health_check extension provides a readiness endpoint on 13133
            var adotConfig = """
                extensions:
                  health_check:
                    endpoint: 0.0.0.0:13133
                receivers:
                  otlp:
                    protocols:
                      http:
                        endpoint: 0.0.0.0:4318
                processors:
                  batch:
                    timeout: 10s
                exporters:
                  awsemf:
                    namespace: TinyNote
                    log_group_name: /aws/otel/tinynote-metrics
                service:
                  extensions: [health_check]
                  pipelines:
                    metrics:
                      receivers: [otlp]
                      processors: [batch]
                      exporters: [awsemf]
                """;

            collectorTaskDef.AddContainer("collector", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("public.ecr.aws/aws-observability/aws-otel-collector:latest"),
                PortMappings = new[]
                {
                    new PortMapping { ContainerPort = 4318 },
                },
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = collectorLogGroup,
                    StreamPrefix = "collector",
                    Mode = AwsLogDriverMode.NON_BLOCKING,
                }),
                Environment = new Dictionary<string, string>
                {
                    ["AOT_CONFIG_CONTENT"] = adotConfig,
                    ["AWS_REGION"] = Region,
                },
            });

            // Collector Fargate service - registered in Cloud Map as an A record.
            // collector.tinynote.local resolves to the collector task's private IP (TTL 10s).
            var collectorService = new FargateService(this, "CollectorService", new FargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = collectorTaskDef,
                DesiredCount = 1,
                SecurityGroups = new[] { collectorTaskSg },
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                AssignPublicIp = false,
            });

            collectorService.EnableCloudMap(new CloudMapOptions
            {
                Name = "collector",
                DnsRecordType = DnsRecordType.A,
                DnsTtl = Duration.Seconds(10),
            });

            // -----------------------------------------------------------------------
            // Frontend and API services
            // -----------------------------------------------------------------------

            // CloudWatch Log Group for Frontend - nginx access/error logs
            var frontendLogGroup = new LogGroup(this, "FrontendLogGroup", new LogGroupProps
            {
                LogGroupName = "/ecs/tinynote/frontend",
                Retention = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            // Application Load Balancer (ALB) - created with FrontendService pattern
            // - Internet-facing ALB in public subnets
            // - Listener on port 80
            // - Path routing: /api* -> API, default -> Frontend
            var frontendService = new ApplicationLoadBalancedFargateService(this, "FrontendService", new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = cluster,
                Cpu = 256,
                MemoryLimitMiB = 512,
                DesiredCount = 1,
                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    Image = ContainerImage.FromDockerImageAsset(FrontendImage),
                    ContainerPort = 80,
                    LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        LogGroup = frontendLogGroup,
                        StreamPrefix = "frontend",
                        Mode = AwsLogDriverMode.NON_BLOCKING,
                    }),
                },
                PublicLoadBalancer = true,
                LoadBalancerName = "tinynote-alb",
                TaskSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                AssignPublicIp = false
            });

            // CloudWatch Log Group for API - captures startup errors and stdout/stderr
            var apiLogGroup = new LogGroup(this, "ApiLogGroup", new LogGroupProps
            {
                LogGroupName = "/ecs/tinynote/api",
                Retention = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            // API target group and Fargate service
            var apiTargetGroup = new ApplicationTargetGroup(this, "ApiTargetGroup", new ApplicationTargetGroupProps
            {
                Vpc = Vpc,
                Port = 8080,
                Protocol = ApplicationProtocol.HTTP,
                TargetType = TargetType.IP,
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Path = "/api/notes",
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5),
                    HealthyHttpCodes = "200,400",
                },
            });

            var apiTaskDef = new FargateTaskDefinition(this, "ApiTaskDef", new FargateTaskDefinitionProps
            {
                Cpu = 512,
                MemoryLimitMiB = 2048,  
                RuntimePlatform = new RuntimePlatform
                {
                    CpuArchitecture = CpuArchitecture.X86_64,
                    OperatingSystemFamily = OperatingSystemFamily.LINUX,
                },
            });

            apiTaskDef.AddContainer("api", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromDockerImageAsset(ApiImage),
                PortMappings = new[] { new PortMapping { ContainerPort = 8080 } },
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = apiLogGroup,
                    StreamPrefix = "api",
                    Mode = AwsLogDriverMode.NON_BLOCKING,
                }),
                Environment = new Dictionary<string, string>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["ASPNETCORE_URLS"] = "http://+:8080",
                    ["ConnectionStrings__DefaultConnection"] = connectionString,
                    ["Cors__AllowedOrigins"] = $"http://{frontendService.LoadBalancer.LoadBalancerDnsName}",
                    // Route 53 private DNS resolves collector.tinynote.local → collector task IP
                    ["OpenTelemetry__OtlpEndpoint"] = "http://collector.tinynote.local:4318",
                },
            });

            // API service - no Service Connect needed.
            // DNS resolves collector.tinynote.local via the Route 53 private hosted zone.
            var apiService = new FargateService(this, "ApiService", new FargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = apiTaskDef,
                DesiredCount = 1,
                SecurityGroups = new[] { EcsTasksSecurityGroup },
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                AssignPublicIp = false,
            });
            apiService.AttachToApplicationTargetGroup(apiTargetGroup);
            Database.Connections.AllowDefaultPortFrom(apiService);

            // Path-based routing: /api* -> API, default -> Frontend
            new ApplicationListenerRule(this, "ApiListenerRule", new ApplicationListenerRuleProps
            {
                Listener = frontendService.Listener,
                Priority = 10,
                Conditions = new[] { ListenerCondition.PathPatterns(new[] { "/api*" }) },
                Action = ListenerAction.Forward(new[] { apiTargetGroup }),
            });

            new CfnOutput(this, "LoadBalancerUrl", new CfnOutputProps
            {
                Value = $"http://{frontendService.LoadBalancer.LoadBalancerDnsName}",
                Description = "TinyNote application URL (ALB)",
            });

            new CfnOutput(this, "ApiLogGroupOutput", new CfnOutputProps
            {
                Value = apiLogGroup.LogGroupName,
                Description = "CloudWatch Log Group for API container (stdout/stderr)",
            });

            new CfnOutput(this, "FrontendLogGroupOutput", new CfnOutputProps
            {
                Value = frontendLogGroup.LogGroupName,
                Description = "CloudWatch Log Group for Frontend container (nginx logs)",
            });

            new CfnOutput(this, "MetricsLogGroupOutput", new CfnOutputProps
            {
                Value = metricsLogGroup.LogGroupName,
                Description = "CloudWatch Log Group for OTEL EMF metrics",
            });
        }
    }
}
