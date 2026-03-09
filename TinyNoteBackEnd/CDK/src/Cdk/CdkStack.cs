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
        internal CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var (vpc, albSg, ecsTasksSg, rdsSg) = CreateNetworking();

            var database = CreateDatabase(vpc, rdsSg);

            var (apiImage, frontendImage) = CreateImageAssets();

            var cluster = CreateCluster(vpc);

            var metricsLogGroup = CreateObservabilityCollector(cluster, vpc, ecsTasksSg);

            var (frontendService, frontendLogGroup) = CreateFrontendService(cluster, frontendImage);

            var apiLogGroup = CreateApiService(cluster, vpc, apiImage, frontendService, database, ecsTasksSg);

            AddOutputs(frontendService, apiLogGroup, frontendLogGroup, metricsLogGroup);
        }

        // VPC with 2 AZs, 1 NAT Gateway; three security groups:
        //   albSg      – internet → ALB (port 80)
        //   ecsTasksSg – ALB → ECS tasks (ports 80 and 8080)
        //   rdsSg      – ECS tasks → RDS (port 5432)
        private (IVpc vpc, ISecurityGroup albSg, ISecurityGroup ecsTasksSg, ISecurityGroup rdsSg) CreateNetworking()
        {
            var vpc = new Vpc(this, "TinyNoteVpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 1,
            });

            var albSg = new SecurityGroup(this, "AlbSecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,
                Description = "Allow HTTP traffic to ALB",
                AllowAllOutbound = true,
            });
            albSg.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(80), "Allow HTTP");

            var ecsTasksSg = new SecurityGroup(this, "EcsTasksSecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,
                Description = "Allow traffic from ALB to ECS tasks",
                AllowAllOutbound = true,
            });
            ecsTasksSg.AddIngressRule(Peer.SecurityGroupId(albSg.SecurityGroupId), Port.Tcp(80),   "Allow HTTP from ALB (frontend)");
            ecsTasksSg.AddIngressRule(Peer.SecurityGroupId(albSg.SecurityGroupId), Port.Tcp(8080), "Allow port 8080 from ALB (API)");

            var rdsSg = new SecurityGroup(this, "RdsSecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,
                Description = "Allow PostgreSQL access from API tasks",
                AllowAllOutbound = false,
            });
            rdsSg.AddIngressRule(Peer.SecurityGroupId(ecsTasksSg.SecurityGroupId), Port.Tcp(5432), "Allow PostgreSQL from API tasks");

            return (vpc, albSg, ecsTasksSg, rdsSg);
        }

        
        // RDS PostgreSQL
        // PostgreSQL 16 in private subnets; credentials passed as plain text for
        // simplicity (use Secrets Manager for production hardening).
        private IDatabaseInstance CreateDatabase(IVpc vpc, ISecurityGroup rdsSg)
        {
            return new DatabaseInstance(this, "TinyNoteDatabase", new DatabaseInstanceProps
            {
                Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps { Version = PostgresEngineVersion.VER_16 }),
                InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO),
                Vpc = vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                SecurityGroups = new[] { rdsSg },
                DatabaseName = "tinynote",
                Credentials = Credentials.FromPassword("postgres", SecretValue.UnsafePlainText("postgres")),
                RemovalPolicy = RemovalPolicy.DESTROY,
            });
        }

        // Docker Image Assets
        // CDK builds both images locally during cdk deploy and pushes them to ECR.
        // Paths are resolved relative to the assembly location so they work when
        // CDK spawns the synthesizer as a subprocess.
        private (DockerImageAsset apiImage, DockerImageAsset frontendImage) CreateImageAssets()
        {
            var assemblyDir  = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var cdkRoot      = Path.GetFullPath(Path.Combine(assemblyDir!, "..", "..", "..", "..", ".."));
            var backendRoot  = Path.GetFullPath(Path.Combine(cdkRoot, ".."));
            var frontendDir  = Path.GetFullPath(Path.Combine(backendRoot, "..", "TinyNoteFrontEnd", "TinyNote"));

            var apiImage = new DockerImageAsset(this, "ApiImage", new DockerImageAssetProps
            {
                Directory = backendRoot,
                File      = "TinyNote.Api/Dockerfile",
                Platform  = Platform_.LINUX_AMD64,
            });

            var frontendImage = new DockerImageAsset(this, "FrontendImage", new DockerImageAssetProps
            {
                Directory = frontendDir,
            });

            return (apiImage, frontendImage);
        }

        //  ECS Cluster
        // Fargate cluster with a Route 53 private DNS namespace (tinynote.local)
        // so internal services can resolve each other by DNS name inside the VPC.
        private Cluster CreateCluster(IVpc vpc)
        {
            return new Cluster(this, "TinyNoteCluster", new ClusterProps
            {
                Vpc         = vpc,
                ClusterName = "tinynote-cluster",
                DefaultCloudMapNamespace = new CloudMapNamespaceOptions
                {
                    Name = "tinynote.local",
                    Type = NamespaceType.DNS_PRIVATE,
                    Vpc  = vpc,
                },
            });
        }

        // Observability – Central ADOT Collector
        // Single Fargate task receives OTLP HTTP from all API instances and exports
        // metrics to CloudWatch via the EMF format.  Registered in Cloud Map as
        // collector.tinynote.local so API tasks can reach it without a proxy.
        private ILogGroup CreateObservabilityCollector(Cluster cluster, IVpc vpc, ISecurityGroup ecsTasksSg)
        {
            var collectorSg = new SecurityGroup(this, "CollectorTaskSg", new SecurityGroupProps
            {
                Vpc         = vpc,
                Description = "Allow OTLP from API tasks to collector",
                AllowAllOutbound = true,
            });
            collectorSg.AddIngressRule(
                Peer.SecurityGroupId(ecsTasksSg.SecurityGroupId),
                Port.Tcp(4318),
                "Allow OTLP HTTP from API tasks");

            var collectorLogGroup = new LogGroup(this, "CollectorLogGroup", new LogGroupProps
            {
                LogGroupName  = "/ecs/tinynote/collector",
                Retention     = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            var metricsLogGroup = new LogGroup(this, "MetricsLogGroup", new LogGroupProps
            {
                LogGroupName  = "/aws/otel/tinynote-metrics",
                Retention     = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

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

            var taskDef = new FargateTaskDefinition(this, "CollectorTaskDef", new FargateTaskDefinitionProps
            {
                Cpu             = 256,
                MemoryLimitMiB  = 512,
                RuntimePlatform = new RuntimePlatform
                {
                    CpuArchitecture       = CpuArchitecture.X86_64,
                    OperatingSystemFamily = OperatingSystemFamily.LINUX,
                },
            });

            taskDef.TaskRole.AddManagedPolicy(
                ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy"));

            taskDef.ObtainExecutionRole().AddManagedPolicy(
                ManagedPolicy.FromAwsManagedPolicyName("AmazonElasticContainerRegistryPublicReadOnly"));

            taskDef.AddContainer("collector", new ContainerDefinitionOptions
            {
                Image        = ContainerImage.FromRegistry("public.ecr.aws/aws-observability/aws-otel-collector:latest"),
                PortMappings = new[] { new PortMapping { ContainerPort = 4318 } },
                Logging      = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup      = collectorLogGroup,
                    StreamPrefix  = "collector",
                    Mode          = AwsLogDriverMode.NON_BLOCKING,
                }),
                Environment = new Dictionary<string, string>
                {
                    ["AOT_CONFIG_CONTENT"] = adotConfig,
                    ["AWS_REGION"]         = Region,
                },
            });

            var service = new FargateService(this, "CollectorService", new FargateServiceProps
            {
                Cluster        = cluster,
                TaskDefinition = taskDef,
                DesiredCount   = 1,
                SecurityGroups = new[] { collectorSg },
                VpcSubnets     = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                AssignPublicIp = false,
            });

            service.EnableCloudMap(new CloudMapOptions
            {
                Name          = "collector",
                DnsRecordType = DnsRecordType.A,
                DnsTtl        = Duration.Seconds(10),
            });

            return metricsLogGroup;
        }

        // Frontend Service and Application Load Balancer
        // ApplicationLoadBalancedFargateService creates the internet-facing ALB,
        // listener on port 80, and the frontend Fargate service in one construct.
        private (ApplicationLoadBalancedFargateService service, ILogGroup logGroup) CreateFrontendService(
            Cluster cluster, DockerImageAsset frontendImage)
        {
            var logGroup = new LogGroup(this, "FrontendLogGroup", new LogGroupProps
            {
                LogGroupName  = "/ecs/tinynote/frontend",
                Retention     = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            var service = new ApplicationLoadBalancedFargateService(this, "FrontendService",
                new ApplicationLoadBalancedFargateServiceProps
                {
                    Cluster          = cluster,
                    Cpu              = 256,
                    MemoryLimitMiB   = 512,
                    DesiredCount     = 1,
                    TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                    {
                        Image     = ContainerImage.FromDockerImageAsset(frontendImage),
                        ContainerPort = 80,
                        LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                        {
                            LogGroup     = logGroup,
                            StreamPrefix = "frontend",
                            Mode         = AwsLogDriverMode.NON_BLOCKING,
                        }),
                    },
                    PublicLoadBalancer = true,
                    LoadBalancerName   = "tinynote-alb",
                    TaskSubnets        = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                    AssignPublicIp     = false,
                });

            return (service, logGroup);
        }

        // API Service
        // Fargate service behind the shared ALB; path /api* is routed here.
        // Connects to RDS via connection string; exports metrics to the ADOT
        // collector via Cloud Map DNS (collector.tinynote.local:4318).
        private ILogGroup CreateApiService(
            Cluster cluster,
            IVpc vpc,
            DockerImageAsset apiImage,
            ApplicationLoadBalancedFargateService frontendService,
            IDatabaseInstance database,
            ISecurityGroup ecsTasksSg)
        {
            var logGroup = new LogGroup(this, "ApiLogGroup", new LogGroupProps
            {
                LogGroupName  = "/ecs/tinynote/api",
                Retention     = RetentionDays.ONE_MONTH,
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

            var connectionString = $"Host={database.InstanceEndpoint.Hostname};Port=5432;Database=tinynote;Username=postgres;Password=postgres";

            var targetGroup = new ApplicationTargetGroup(this, "ApiTargetGroup", new ApplicationTargetGroupProps
            {
                Vpc         = vpc,
                Port        = 8080,
                Protocol    = ApplicationProtocol.HTTP,
                TargetType  = TargetType.IP,
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Path             = "/api/notes",
                    Interval         = Duration.Seconds(30),
                    Timeout          = Duration.Seconds(5),
                    HealthyHttpCodes = "200,400",
                },
            });

            var taskDef = new FargateTaskDefinition(this, "ApiTaskDef", new FargateTaskDefinitionProps
            {
                Cpu             = 512,
                MemoryLimitMiB  = 2048,
                RuntimePlatform = new RuntimePlatform
                {
                    CpuArchitecture       = CpuArchitecture.X86_64,
                    OperatingSystemFamily = OperatingSystemFamily.LINUX,
                },
            });

            taskDef.AddContainer("api", new ContainerDefinitionOptions
            {
                Image        = ContainerImage.FromDockerImageAsset(apiImage),
                PortMappings = new[] { new PortMapping { ContainerPort = 8080 } },
                Logging      = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup     = logGroup,
                    StreamPrefix = "api",
                    Mode         = AwsLogDriverMode.NON_BLOCKING,
                }),
                Environment = new Dictionary<string, string>
                {
                    ["ASPNETCORE_ENVIRONMENT"]              = "Production",
                    ["ASPNETCORE_URLS"]                     = "http://+:8080",
                    ["ConnectionStrings__DefaultConnection"] = connectionString,
                    ["Cors__AllowedOrigins"]                = $"http://{frontendService.LoadBalancer.LoadBalancerDnsName}",
                    ["OpenTelemetry__OtlpEndpoint"]         = "http://collector.tinynote.local:4318",
                },
            });

            var apiService = new FargateService(this, "ApiService", new FargateServiceProps
            {
                Cluster        = cluster,
                TaskDefinition = taskDef,
                DesiredCount   = 1,
                SecurityGroups = new[] { ecsTasksSg },
                VpcSubnets     = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                AssignPublicIp = false,
            });

            apiService.AttachToApplicationTargetGroup(targetGroup);
            database.Connections.AllowDefaultPortFrom(apiService);

            // Step 6 – ALB routing: /api* → API, default → Frontend
            new ApplicationListenerRule(this, "ApiListenerRule", new ApplicationListenerRuleProps
            {
                Listener   = frontendService.Listener,
                Priority   = 10,
                Conditions = new[] { ListenerCondition.PathPatterns(new[] { "/api*" }) },
                Action     = ListenerAction.Forward(new[] { targetGroup }),
            });

            return logGroup;
        }

        //  CloudFormation Outputs
        private void AddOutputs(
            ApplicationLoadBalancedFargateService frontendService,
            ILogGroup apiLogGroup,
            ILogGroup frontendLogGroup,
            ILogGroup metricsLogGroup)
        {
            new CfnOutput(this, "LoadBalancerUrl", new CfnOutputProps
            {
                Value       = $"http://{frontendService.LoadBalancer.LoadBalancerDnsName}",
                Description = "TinyNote application URL",
            });

            new CfnOutput(this, "ApiLogGroupOutput", new CfnOutputProps
            {
                Value       = apiLogGroup.LogGroupName,
                Description = "CloudWatch Log Group for API container",
            });

            new CfnOutput(this, "FrontendLogGroupOutput", new CfnOutputProps
            {
                Value       = frontendLogGroup.LogGroupName,
                Description = "CloudWatch Log Group for Frontend container",
            });

            new CfnOutput(this, "MetricsLogGroupOutput", new CfnOutputProps
            {
                Value       = metricsLogGroup.LogGroupName,
                Description = "CloudWatch Log Group for OTEL EMF metrics",
            });
        }
    }
}
