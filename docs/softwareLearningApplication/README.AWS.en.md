<h1 align="center">AWS — Amazon Web Services</h1>

<p align="center">
  <em>Complete guide: what AWS is, service catalog, pricing and AWS CLI commands per service</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/AWS-FF9900?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="AWS">
  <img src="https://img.shields.io/badge/200%2B_Services-232F3E?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="200+ Services">
  <img src="https://img.shields.io/badge/Pay_as_you_go-00A1D6?style=for-the-badge" alt="Pay as you go">
  <img src="https://img.shields.io/badge/AWS_CLI-3B82F6?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="AWS CLI">
</p>

---

## Table of Contents

1. [What is AWS?](#what-is-aws)
2. [Global infrastructure: regions and availability zones](#global-infrastructure-regions-and-availability-zones)
3. [AWS pricing model](#aws-pricing-model)
4. [AWS Free Tier](#aws-free-tier)
5. [AWS CLI: installation and configuration](#aws-cli-installation-and-configuration)
6. [Service categories](#service-categories)
7. [Compute](#compute)
   - [EC2](#ec2)
   - [AWS Lambda](#aws-lambda)
   - [Amazon ECS](#amazon-ecs)
   - [Amazon EKS](#amazon-eks)
   - [AWS Fargate](#aws-fargate)
   - [Elastic Beanstalk](#elastic-beanstalk)
8. [Storage](#storage)
   - [S3](#s3)
   - [EBS](#ebs)
   - [EFS](#efs)
   - [S3 Glacier](#s3-glacier)
9. [Databases](#databases)
   - [Amazon RDS](#amazon-rds)
   - [Amazon Aurora](#amazon-aurora)
   - [DynamoDB](#dynamodb)
   - [ElastiCache](#elasticache)
   - [Amazon Redshift](#amazon-redshift)
10. [Networking and content delivery](#networking-and-content-delivery)
    - [Amazon VPC](#amazon-vpc)
    - [Route 53](#route-53)
    - [CloudFront](#cloudfront)
    - [API Gateway](#api-gateway)
    - [Elastic Load Balancing](#elastic-load-balancing)
    - [NAT Gateway](#nat-gateway)
11. [Messaging and integration](#messaging-and-integration)
    - [SQS](#sqs)
    - [SNS](#sns)
    - [EventBridge](#eventbridge)
    - [Kinesis](#kinesis)
    - [Step Functions](#step-functions)
12. [Security, identity, and compliance](#security-identity-and-compliance)
    - [IAM](#iam)
    - [AWS KMS](#aws-kms)
    - [Secrets Manager](#secrets-manager)
    - [SSM Parameter Store](#ssm-parameter-store)
    - [Amazon Cognito](#amazon-cognito)
    - [AWS WAF](#aws-waf)
13. [Management and governance](#management-and-governance)
    - [CloudWatch](#cloudwatch)
    - [CloudTrail](#cloudtrail)
    - [CloudFormation](#cloudformation)
    - [AWS Config](#aws-config)
    - [AWS Budgets](#aws-budgets)
14. [Analytics](#analytics)
    - [Athena](#athena)
    - [AWS Glue](#aws-glue)
    - [Amazon EMR](#amazon-emr)
15. [Machine Learning and AI](#machine-learning-and-ai)
    - [Amazon SageMaker](#amazon-sagemaker)
    - [Amazon Bedrock](#amazon-bedrock)
16. [Development and DevOps tools](#development-and-devops-tools)
17. [Services used in this project](#services-used-in-this-project)
18. [References](#references)

---

## What is AWS?

**Amazon Web Services (AWS)** is the largest and most widely adopted cloud computing platform in the world. Launched in 2006, it offers **more than 200 services** ranging from basic infrastructure (servers, storage, networking) to artificial intelligence, machine learning, IoT, analytics, and quantum computing services.

Instead of buying and maintaining physical servers, with AWS you rent computing resources on demand, paying **only for what you use**, with no long-term contracts and the ability to scale from 0 to thousands of servers in minutes.

| Characteristic | Detail |
|----------------|--------|
| **Launch** | 2006 |
| **Services** | More than 200 services in 30+ categories |
| **Regions** | 30+ regions and 90+ availability zones worldwide |
| **Billing model** | Pay-as-you-go (pay per use), per second or hour |
| **Market share** | Cloud market leader (~30% global share) |
| **Free tier** | Free Tier: always free, 12 months free, and free trials |

### What problems does it solve?

| Traditional problem | AWS solution |
|---------------------|--------------|
| Buying servers months in advance | **EC2 / Lambda**: provision capacity in minutes |
| Maintaining and patching infrastructure | **Managed services**: AWS manages hardware, patches, and high availability |
| Unpredictable traffic spikes | **Auto Scaling**: scales automatically on demand |
| High initial costs (CapEx) | **Pay-as-you-go**: turns fixed cost into variable cost (OpEx) |
| Complex backup and disaster recovery | **Snapshots, RDS Multi-AZ, S3 replication** built-in |
| Security and compliance | **IAM, KMS, CloudTrail, WAF** with ISO, SOC, HIPAA certifications |

### Key advantages

- **Elasticity**: scale up or down automatically based on load.
- **Reliability**: redundant infrastructure across multiple availability zones.
- **Security**: encryption, managed identity, and audit by default.
- **Cost**: you only pay for actual use, with discounts for commitment.
- **Innovation**: new services and features launched almost daily.

---

## Global infrastructure: regions and availability zones

AWS operates **data centers** organized hierarchically:

```
Region (us-east-1, eu-west-1, ap-southeast-1...)
└── Availability Zones (AZs): 3-6 per region
    └── Isolated data centers
        └── Redundant servers, storage, and networking
```

- **Region**: geographic area (e.g., `us-east-1` N. Virginia, `eu-west-1` Ireland, `sa-east-1` São Paulo). Each region is fully independent.
- **Availability Zone (AZ)**: one or more isolated data centers (independent power, network, and cooling) within a region. AZs are connected by low-latency fiber.
- **Point of Presence (Edge)**: global CloudFront network for low-latency content delivery.

> **Rule of thumb:** deploy your applications in **at least 2 AZs** of the same region for high availability. Choose the region based on **proximity to your users**, legal data-residency requirements, and **pricing** (not all regions cost the same).

```bash
# List available regions and AZs
aws ec2 describe-regions
aws ec2 describe-availability-zones --region us-east-1
```

---

## AWS pricing model

AWS charges based on a combination of **four axes**:

| Cost axis | Examples | Typical unit |
|-----------|----------|--------------|
| **Compute time** | EC2, Lambda, EKS | Per second / per hour / per GB-second |
| **Storage** | S3, EBS, RDS | Per GB per month |
| **Network (transfer)** | Outbound data, NAT Gateway, CloudFront | Per GB transferred |
| **Requests (API)** | DynamoDB, SQS, SNS, API Gateway | Per million requests |

### Payment modes

| Mode | What it is | Discount |
|------|------------|----------|
| **On-Demand** | Pay what you use, no commitment | Base price |
| **Savings Plans** | Usage commitment ($/h) for 1-3 years | 30-60% (1 year), 50-72% (3 years) |
| **Reserved Instances** | Instances reserved for 1-3 years | Up to 72% |
| **Spot Instances** | AWS spare capacity at variable price | 60-90% (can be reclaimed with 2 min notice) |
| **Free Tier** | Limited free usage | 100% |

### Hidden costs that surprise the most

- **Outbound data transfer**: `$0.09/GB` (first 10 TB) in `us-east-1`. It is the most underestimated cost.
- **NAT Gateway**: `$0.045/hour` + `$0.045/GB` — just the fixed cost is ~`$32.85/month` per gateway.
- **EBS snapshots**: `$0.05/GB-month`.
- **CloudWatch Logs**: `$0.50/GB` ingested + `$0.03/GB` stored.
- **Load Balancers**: per-hour cost + per-LCU, even with no traffic.

> **Tip:** use the [AWS Pricing Calculator](https://calculator.aws/) before deploying anything. Calculator and budgets in [AWS Budgets](#aws-budgets).

---

## AWS Free Tier

| Category | What it includes | Duration |
|----------|------------------|----------|
| **Always free** | 1M Lambda requests/month, 25 GB DynamoDB, 1M SQS messages/month, 1 TB CloudFront/month, IAM/VPC/Auto Scaling (all free), 10,000 standard SSM parameters | No limit |
| **12 months free** | 750 h/month EC2 `t2/t3.micro`, 5 GB S3 Standard, 750 h/month RDS `db.t2/t3.micro`, 100 GB outbound/month | From sign-up |
| **Free trials** | 1 month of product trials: Redshift, SageMaker, EMR, etc. | 30 days |

```bash
# Check your Free Tier usage and costs
aws ce get-cost-and-usage --time-period Start=$(Get-Date -Format yyyy-MM-01),End=$(Get-Date -AddMonths 1 -Format yyyy-MM-01) --granularity MONTHLY --metrics "UnblendedCost"
```

---

## AWS CLI: installation and configuration

The **AWS CLI** is the command-line tool to interact with every AWS service. It is essential for automation, scripting, and deployments.

```bash
# Installation (Windows / macOS / Linux)
# - Windows: download the MSI installer from https://aws.amazon.com/cli/
# - macOS: brew install awscli
# - Linux: curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"

# Verify installation
aws --version

# Initial configuration (Access Key, Secret Key, Region, Output format)
aws configure

# Configure multiple profiles (e.g., dev, prod, local)
aws configure --profile prod

# Verify the identity you are authenticated as
aws sts get-caller-identity
```

### Key CLI concepts

| Concept | Description |
|----------|-------------|
| `--region` | Region the command runs against |
| `--profile` | Credentials profile to use |
| `--output json\|table\|text` | Output format |
| `--endpoint-url` | Point to a different endpoint (used by MiniStack/LocalStack) |
| `--query` | Filter the JSON output (JMESPath) |
| `--dry-run` | Validate the command without running it |

```bash
# Cross-cutting useful commands
aws help                              # General help
aws <service> help                    # Service help (e.g., aws s3 help)
aws ec2 describe-regions --output table
aws sts get-caller-identity
```

> **To develop locally without a real account:** MiniStack emulates AWS services at `http://localhost:4566`. Use `--endpoint-url=http://localhost:4566` on each command. See [README.Ministack.md](README.Ministack.md).

---

## Service categories

AWS groups its services into categories by function:

| Category | Function | Examples |
|----------|----------|----------|
| **Compute** | Processing power (VMs, containers, serverless) | EC2, Lambda, ECS, EKS, Fargate |
| **Storage** | Store and retrieve data | S3, EBS, EFS, Glacier |
| **Databases** | Relational and NoSQL databases | RDS, Aurora, DynamoDB, Redshift |
| **Networking & CDN** | Connectivity, DNS, load balancing, CDN | VPC, Route 53, CloudFront, API Gateway, ELB |
| **Messaging & integration** | Communication between services | SQS, SNS, EventBridge, Kinesis, Step Functions |
| **Security & identity** | Authentication, encryption, permissions | IAM, KMS, Secrets Manager, Cognito, WAF |
| **Management & governance** | Monitoring, audit, orchestration | CloudWatch, CloudTrail, CloudFormation, Config |
| **Analytics** | Data processing and analysis | Athena, Glue, EMR, Kinesis |
| **Machine Learning / AI** | AI models and applications | SageMaker, Bedrock |
| **Development tools** | CI/CD, repositories, deployments | CodeCommit, CodeBuild, CodePipeline, CodeDeploy |

Below, each category with its main services: **what it is**, **what it is used for**, **how much it costs**, and **how to manage it with the AWS CLI**.

---

## Compute

### EC2

> **Amazon Elastic Compute Cloud** — virtual servers (VMs) in the cloud, the foundational AWS service.

- **What is it?** Virtual machines with hundreds of instance types optimized for compute, memory, GPU, or storage.
- **Use cases:** web servers, self-managed databases, batch processing, development environments.
- **Price:** from `$0.0042/h` (`t4g.nano` Graviton) or `$0.0104/h` (`t3.micro` x86). Spot up to 90% cheaper.

```bash
# Launch an instance (Windows Server / Ubuntu / Amazon Linux)
aws ec2 run-instances --image-id ami-0abcdef1234567890 --instance-type t3.micro --key-name my-key --security-group-ids sg-123 --subnet-id subnet-123

# List instances and their state
aws ec2 describe-instances --query 'Reservations[].Instances[].[InstanceId,State.Name,PublicIpAddress]' --output table

# Stop / start / reboot / terminate (careful with terminate!)
aws ec2 stop-instances --instance-ids i-1234567890abcdef0
aws ec2 start-instances --instance-ids i-1234567890abcdef0
aws ec2 reboot-instances --instance-ids i-1234567890abcdef0
aws ec2 terminate-instances --instance-ids i-1234567890abcdef0

# Key pairs for SSH/RDP access
aws ec2 create-key-pair --key-name my-key --query 'KeyMaterial' --output text > my-key.pem
aws ec2 describe-key-pairs
aws ec2 delete-key-pair --key-name my-key

# Security groups (firewall)
aws ec2 create-security-group --group-name web --description "Web traffic" --vpc-id vpc-123
aws ec2 authorize-security-group-ingress --group-id sg-123 --protocol tcp --port 80 --cidr 0.0.0.0/0
aws ec2 describe-security-groups

# Snapshots (disk backups)
aws ec2 create-snapshot --volume-id vol-123 --description "Daily backup"
aws ec2 describe-snapshots
```

### AWS Lambda

> **Serverless computing** — runs code without managing servers. You only pay for execution.

- **What is it?** Runs code functions (Node, Python, Java, Go, .NET, etc.) in response to events, without provisioning servers.
- **Use cases:** serverless APIs, event processing, scheduled tasks (cron), integrations, microservices.
- **Price:** `$0.20/1M` requests + `$0.0000166667/GB-second`. First million requests and 400,000 GB-s **free** every month.

```bash
# Package the code and create the function
aws lambda create-function \
  --function-name my-function \
  --runtime python3.12 \
  --role arn:aws:iam::123456789012:role/lambda-basic-execution \
  --handler lambda_function.lambda_handler \
  --zip-file fileb://function.zip

# Invoke
aws lambda invoke --function-name my-function --payload '{"key":"value"}' response.json

# List / update / delete
aws lambda list-functions
aws lambda update-function-code --function-name my-function --zip-file fileb://function.zip
aws lambda update-function-configuration --function-name my-function --memory-size 512
aws lambda delete-function --function-name my-function

# View invocation logs
aws logs tail /aws/lambda/my-function --follow
```

### Amazon ECS

> **Elastic Container Service** — runs Docker containers without managing Kubernetes.

- **What is it?** Managed container orchestrator. Runs tasks and services on EC2 clusters or with Fargate.
- **Use cases:** containerized microservices, 12-factor applications, blue/green deployments.
- **Price:** the control plane is **free**; you pay for compute (EC2 or Fargate).

```bash
# Create a cluster
aws ecs create-cluster --cluster-name my-cluster
aws ecs list-clusters

# Register a task definition (the container "recipe")
aws ecs register-task-definition \
  --family my-app \
  --cpu 256 --memory 512 \
  --network-mode awsvpc \
  --container-definitions '[{"name":"app","image":"nginx:latest","portMappings":[{"containerPort":80}]}]'

# Run a one-off task
aws ecs run-task --cluster my-cluster --task-definition my-app --count 1

# Create a service (a task that keeps running)
aws ecs create-service \
  --cluster my-cluster \
  --service-name my-service \
  --task-definition my-app \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-123],securityGroups=[sg-123]}"

# View services and tasks
aws ecs list-services --cluster my-cluster
aws ecs list-tasks --cluster my-cluster
```

### Amazon EKS

> **Elastic Kubernetes Service** — Kubernetes managed by AWS.

- **What is it?** Kubernetes as a service: AWS manages the control plane; you manage the nodes.
- **Use cases:** workloads already using Kubernetes, CNCF ecosystem, workloads needing fine-grained control.
- **Price:** `$0.10/h` per cluster (~`$73/month`) + EC2 or Fargate nodes.

```bash
# Create an EKS cluster
aws eks create-cluster --name my-cluster --role-arn arn:aws:iam::123456789012:role/eks-cluster-role --resources-vpc-config subnetIds=subnet-123,subnet-456
aws eks list-clusters
aws eks describe-cluster --name my-cluster

# Configure kubectl to talk to the cluster
aws eks update-kubeconfig --region us-east-1 --name my-cluster
kubectl get nodes
```

### AWS Fargate

> **Serverless engine for containers** — runs containers without managing servers or nodes.

- **What is it?** Compute layer for ECS and EKS that removes node management. You define CPU/memory and Fargate runs it.
- **Price:** `$0.04048/vCPU/h` + `$0.004445/GB/h`.

```bash
# Fargate is used through ECS (launch-type FARGATE) or EKS
aws ecs create-service --launch-type FARGATE ...   # see example in ECS

# View Fargate tasks
aws ecs list-tasks --cluster my-cluster
```

### Elastic Beanstalk

> **AWS PaaS** — deploys applications without thinking about infrastructure.

- **What is it?** Service that automatically provisions (EC2, ALB, Auto Scaling, RDS) based on a chosen platform. Ideal for getting started fast.
- **Price:** free; you only pay for the underlying resources.

```bash
# Create and deploy an application
aws elasticbeanstalk create-application --application-name my-app
aws elasticbeanstalk create-environment \
  --application-name my-app \
  --environment-name my-app-prod \
  --solution-stack-name "64bit Amazon Linux 2023 v6.1.0 running Python 3.11"
# (deploy code: eb deploy from the Beanstalk CLI)

# View environments and status
aws elasticbeanstalk describe-environments
aws elasticbeanstalk list-platform-versions
```

---

## Storage

### S3

> **Simple Storage Service** — object storage (files, images, backups, logs) with 99.999999999% durability (11 nines).

- **What is it?** Stores "objects" (files) inside "buckets" (containers), organized by "keys" (paths). No size limit.
- **Use cases:** static hosting, data lakes, backups, media distribution, log storage.
- **Price:** `$0.023/GB-month` (Standard, first 50 TB). Requests: `$0.005/1k` PUT, `$0.0004/1k` GET.

```bash
# Create a bucket (names are globally unique)
aws s3 mb s3://my-bucket --region us-east-1
aws s3 ls                          # list buckets

# Copy / move / delete / sync
aws s3 cp file.txt s3://my-bucket/
aws s3 cp s3://my-bucket/file.txt ./
aws s3 mv s3://my-bucket/file.txt s3://my-bucket/renamed.txt
aws s3 rm s3://my-bucket/file.txt
aws s3 sync ./folder s3://my-bucket/folder
aws s3 ls s3://my-bucket --recursive

# Generate a presigned URL (temporary access to an object)
aws s3 presign s3://my-bucket/file.txt --expires-in 3600

# Advanced API (versioning, policies, lifecycle)
aws s3api create-bucket --bucket my-bucket --region us-east-1
aws s3api put-bucket-versioning --bucket my-bucket --versioning-configuration Status=Enabled
aws s3api put-bucket-policy --bucket my-bucket --policy file://policy.json
aws s3api put-bucket-lifecycle-configuration --bucket my-bucket --lifecycle-configuration file://lifecycle.json
aws s3api get-bucket-encryption --bucket my-bucket
```

### EBS

> **Elastic Block Store** — persistent block storage attached to EC2 instances.

- **What is it?** SSD/HDD storage volumes attached to an EC2 instance (like a virtual hard drive).
- **Use cases:** instance filesystems, self-managed databases, boot volumes.
- **Price:** `$0.08/GB-month` (`gp3`, 3,000 IOPS and 125 MB/s included). Snapshots `$0.05/GB-month`.

```bash
# Create a volume and attach it to an instance
aws ec2 create-volume --availability-zone us-east-1a --size 100 --volume-type gp3
aws ec2 attach-volume --volume-id vol-123 --instance-id i-123 --device /dev/sdf

# List / modify / delete
aws ec2 describe-volumes
aws ec2 modify-volume --volume-id vol-123 --size 200
aws ec2 delete-volume --volume-id vol-123

# Snapshots
aws ec2 create-snapshot --volume-id vol-123 --description "Backup"
aws ec2 describe-snapshots
aws ec2 copy-snapshot --source-region us-east-1 --source-snapshot-id snap-123 --description "Copy"
aws ec2 delete-snapshot --snapshot-id snap-123
```

### EFS

> **Elastic File System** — shared filesystem (NFS) for multiple EC2 instances simultaneously.

- **What is it?** Managed, elastic, shared file storage. Mounted via NFS.
- **Use cases:** shared storage between containers/microservices, home directories, code repositories.
- **Price:** from `$0.30/GB-month` (Standard). No provisioning cost.

```bash
# Create a filesystem
aws efs create-file-system --creation-token my-efs --performance-mode generalPurpose
aws efs describe-file-systems

# Create mount targets (in each AZ)
aws efs create-mount-target --file-system-id fs-123 --subnet-id subnet-123 --security-groups sg-123
aws efs describe-mount-targets --file-system-id fs-123

# Mount on an EC2 instance
# sudo mount -t nfs4 -o nfsvers=4.1,rsize=1048576,wsize=1048576 fs-123.efs.us-east-1.amazonaws.com:/ /mnt/efs
```

### S3 Glacier

> **Archive storage (cold storage)** — the cheapest AWS storage for data that is almost never accessed.

- **What is it?** Storage classes inside S3 for archive data with recovery times from minutes to hours.
- **Use cases:** legal archives, audits, long-retention backups, regulated data.
- **Price:** Glacier Flexible `$0.0036/GB-month`; Glacier Deep Archive `$0.00099/GB-month` (the cheapest).

```bash
# Vaults (archive containers)
aws glacier create-vault --account-id - --vault-name my-vault
aws glacier list-vaults --account-id -
aws glacier upload-archive --account-id - --vault-name my-vault --body archive.zip

# The modern way: use S3 with the GLACIER storage class
aws s3api put-object --bucket my-bucket --key backup.zip --storage-class GLACIER
aws s3api copy-object --bucket my-bucket --key backup.zip --copy-source my-bucket/backup.zip --storage-class DEEP_ARCHIVE

# Retrieve an object from Glacier (restore)
aws s3api restore-object --bucket my-bucket --key backup.zip --restore-request '{"Days":7,"GlacierJobParameters":{"Tier":"Standard"}}'
```

---

## Databases

### Amazon RDS

> **Relational Database Service** — managed relational databases (MySQL, PostgreSQL, MariaDB, Oracle, SQL Server).

- **What is it?** Deploys and manages the database automatically: patches, backups, replicas, and failover.
- **Use cases:** applications with relational data, migrations from on-premises SQL Server/MySQL/Postgres, transactional systems.
- **Price:** `db.t4g.micro` ~`$0.016/h`. **Multi-AZ** doubles the cost (high availability).

```bash
# Create a database instance
aws rds create-db-instance \
  --db-instance-identifier my-db \
  --db-instance-class db.t4g.micro \
  --engine mysql \
  --allocated-storage 20 \
  --master-username admin --master-user-password "MySecurePassword!123"

# View / modify / delete
aws rds describe-db-instances
aws rds modify-db-instance --db-instance-identifier my-db --db-instance-class db.t4g.small --apply-immediately
aws rds delete-db-instance --db-instance-identifier my-db --skip-final-snapshot

# Snapshots and restore
aws rds create-db-snapshot --db-instance-identifier my-db --db-snapshot-identifier my-db-backup
aws rds describe-db-snapshots
aws rds restore-db-instance-from-db-snapshot --db-instance-identifier my-db-restored --db-snapshot-identifier my-db-backup

# Read replicas
aws rds create-db-instance-read-replica --db-instance-identifier my-db-read --source-db-instance-identifier my-db
```

### Amazon Aurora

> **MySQL/PostgreSQL-compatible relational database**, 5x faster, at cloud scale.

- **What is it?** AWS database engine (compatible with MySQL and PostgreSQL) with high performance, replication, and automatic scaling.
- **Use cases:** applications that need high-availability performance with MySQL/Postgres compatibility.
- **Price:** `$0.10/GB-month` + `$0.20/1M` I/O. `Aurora Serverless v2` scales automatically.

```bash
# Create an Aurora cluster (PostgreSQL or MySQL)
aws rds create-db-cluster \
  --db-cluster-identifier aurora-postgres \
  --engine aurora-postgresql \
  --engine-version 15.4 \
  --master-username admin --master-user-password "MySecurePassword!123"

# Add read/write instances
aws rds create-db-instance --db-instance-identifier aurora-postgres-inst1 --db-cluster-identifier aurora-postgres --engine aurora-postgresql --db-instance-class db.r5.large

# View clusters
aws rds describe-db-clusters
aws rds describe-db-instances --filters "Name=db-cluster-id,Values=aurora-postgres"
```

### DynamoDB

> **Serverless NoSQL database** for key-value and documents, with single-digit millisecond latency.

- **What is it?** Fully managed NoSQL database that scales automatically without servers.
- **Use cases:** user sessions, carts, lookup tables, IoT, high-scale applications.
- **Price:** On-demand `$0.25/1M` reads and `$1.25/1M` writes. **25 GB of storage always free.**

```bash
# Create table
aws dynamodb create-table \
  --table-name Users \
  --attribute-definitions AttributeName=id,AttributeType=S \
  --key-schema AttributeName=id,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST
aws dynamodb list-tables

# CRUD
aws dynamodb put-item --table-name Users --item '{"id":{"S":"1"},"name":{"S":"Ana"},"age":{"N":"30"}}'
aws dynamodb get-item --table-name Users --key '{"id":{"S":"1"}}'
aws dynamodb update-item --table-name Users --key '{"id":{"S":"1"}}' --update-expression "SET age = :n" --expression-attribute-values '{":n":{"N":"31"}}'
aws dynamodb delete-item --table-name Users --key '{"id":{"S":"1"}}'

# Query and Scan
aws dynamodb query --table-name Users --key-condition-expression "id = :id" --expression-attribute-values '{":id":{"S":"1"}}'
aws dynamodb scan --table-name Users

# TTL (automatic expiration)
aws dynamodb update-time-to-live --table-name Users --time-to-live-specification "Enabled=true,AttributeName=expires"
```

### ElastiCache

> **Managed in-memory cache** — Redis, Valkey, or Memcached as a service.

- **What is it?** Distributed cache memory managed by AWS.
- **Use cases:** database caching, sessions, rate limiting, lightweight queues, leaderboards.
- **Price:** from ~`$0.017/h` (`cache.t3.micro`).

```bash
# Create a Redis cache cluster
aws elasticache create-cache-cluster --cache-cluster-id my-cache --cache-node-type cache.t3.micro --engine redis --num-cache-nodes 1
aws elasticache describe-cache-clusters

# Replication group (high availability)
aws elasticache create-replication-group --replication-group-id my-cache-ha --replication-group-description "Redis HA" --cache-node-type cache.t3.micro --engine redis --num-cache-clusters 2

# Verify and delete
aws elasticache describe-replication-groups
aws elasticache delete-cache-cluster --cache-cluster-id my-cache
```

### Amazon Redshift

> **Data warehouse** for large-scale analytics on petabytes of data.

- **What is it?** Columnar data warehouse optimized for complex analytical queries on large volumes.
- **Use cases:** reporting, BI, data warehousing, historical analysis.
- **Price:** from ~`$0.25/h` (dense nodes). Pay per node + storage.

```bash
# Create a cluster
aws redshift create-cluster \
  --cluster-identifier my-wh \
  --node-type dc2.large \
  --number-of-nodes 2 \
  --master-username admin --master-user-password "MySecurePassword!123"

aws redshift describe-clusters
aws redshift delete-cluster --cluster-identifier my-wh --skip-final-cluster-snapshot

# Run SQL (using the DB endpoint)
# psql -h <endpoint.redshift.amazonaws.com> -U admin -d dev
```

---

## Networking and content delivery

### Amazon VPC

> **Virtual Private Cloud** — your own isolated private network inside AWS.

- **What is it?** Your own virtual network with IPs, subnets, route tables, gateways, and firewalls. Everything is deployed inside a VPC.
- **Use cases:** isolate environments, control inbound/outbound traffic, connect to on-premises (VPN/Direct Connect).
- **Price:** **free** (you only pay for components like NAT Gateway or VPN).

```bash
# Base network
aws ec2 create-vpc --cidr-block 10.0.0.0/16
aws ec2 create-subnet --vpc-id vpc-123 --cidr-block 10.0.1.0/24 --availability-zone us-east-1a
aws ec2 create-internet-gateway
aws ec2 attach-internet-gateway --internet-gateway-id igw-123 --vpc-id vpc-123

# Route table: give public subnets Internet access
aws ec2 create-route-table --vpc-id vpc-123
aws ec2 create-route --route-table-id rtb-123 --destination-cidr-block 0.0.0.0/0 --gateway-id igw-123
aws ec2 associate-route-table --route-table-id rtb-123 --subnet-id subnet-123

# NAT Gateway (outbound Internet for private subnets, no public IP)
aws ec2 allocate-address --domain vpc
aws ec2 create-nat-gateway --subnet-id subnet-123 --allocation-id eipalloc-123

# VPC Endpoints (private connection to S3/DynamoDB without going through the Internet)
aws ec2 create-vpc-endpoint --vpc-id vpc-123 --service-name com.amazonaws.us-east-1.s3 --route-table-ids rtb-123

# Peering between VPCs
aws ec2 create-vpc-peering-connection --vpc-id vpc-123 --peer-vpc-id vpc-456
```

### Route 53

> **Managed DNS** — register domains and resolve domain names.

- **What is it?** Scalable DNS service and domain registration.
- **Use cases:** high-availability DNS, latency-based routing, failover, health checks, domain registration.
- **Price:** `$0.50/hosted-zone/month` + per-query cost (~`$0.40-0.60/M`).

```bash
# Create a hosted zone
aws route53 create-hosted-zone --name mydomain.com --caller-reference ref-001
aws route53 list-hosted-zones

# Add DNS records (A, CNAME, MX, TXT, etc.)
aws route53 change-resource-record-sets \
  --hosted-zone-id Z123456789 \
  --change-batch '{"Changes":[{"Action":"UPSERT","ResourceRecordSet":{"Name":"www.mydomain.com","Type":"A","TTL":300,"ResourceRecords":[{"Value":"1.2.3.4"}]}}]}'

# View records and changes
aws route53 list-resource-record-sets --hosted-zone-id Z123456789
aws route53 get-change --id C123456789

# Health check
aws route53 create-health-check --caller-reference hc-001 --health-check-config '{"Type":"HTTP","IPAddress":"1.2.3.4","Port":80}'
```

### CloudFront

> **CDN (Content Delivery Network)** — distributes static and dynamic content globally.

- **What is it?** Edge network that caches and serves content from the points of presence closest to the user.
- **Use cases:** accelerate websites, serve media, global APIs, protect with WAF, managed SSL.
- **Price:** `$0.085/GB` of Internet egress (1 TB free every month).

```bash
# Create a distribution (with S3 origin)
aws cloudfront create-distribution \
  --distribution-config '{"CallerReference":"ref-001","Comment":"My CDN","Origins":{"Quantity":1,"Items":[{"Id":"S3-my-bucket","DomainName":"my-bucket.s3.amazonaws.com","S3OriginConfig":{"OriginAccessIdentity":""}}]},"DefaultCacheBehavior":{"TargetOriginId":"S3-my-bucket","ViewerProtocolPolicy":"redirect-to-https","ForwardedValues":{"QueryString":false,"Cookies":{"Forward":"none"}},"MinTTL":0,"TrustedSigners":{"Enabled":false,"Quantity":0}},"Enabled":true}'

aws cloudfront list-distributions
aws cloudfront get-distribution --id E1234567890ABC

# Invalidate the cache
aws cloudfront create-invalidation --distribution-id E1234567890ABC --paths "/*"
aws cloudfront list-invalidations --distribution-id E1234567890ABC
```

### API Gateway

> **Creates and publishes REST and HTTP APIs** in a managed, scalable way.

- **What is it?** Managed entry point for your APIs. Integrates with Lambda, EC2, HTTP, and more.
- **Use cases:** serverless APIs, authentication layer, rate limiting, API versioning.
- **Price:** REST `$3.50/M` requests; HTTP `$1.00/M`; WebSocket `$1.00/M` (connections) + `$0.25/M` messages.

```bash
# REST API (v1)
aws apigateway create-rest-api --name "My API"
aws apigateway get-resources --rest-api-id abc123
aws apigateway put-method --rest-api-id abc123 --resource-id / --http-method GET --authorization-type NONE
aws apigateway put-integration --rest-api-id abc123 --resource-id / --http-method GET --type MOCK
aws apigateway create-deployment --rest-api-id abc123 --stage-name prod

# HTTP API (v2)
aws apigatewayv2 create-api --name "My HTTP API" --protocol-type HTTP
aws apigatewayv2 create-stage --api-id abcdef --stage-name prod
aws apigatewayv2 get-apis
```

### Elastic Load Balancing

> **Load balancing** — distributes traffic among instances, containers, or Lambdas.

- **What is it?** ALB (layer 7, HTTP/HTTPS), NLB (layer 4, TCP/UDP), CLB (legacy).
- **Use cases:** distribute web traffic, expose microservices, automatic failover.
- **Price:** ALB `$0.0225/h` + `$0.008/LCU-h`; NLB `$0.0225/h` + `$0.006/LCU-h`.

```bash
# Create an ALB
aws elbv2 create-load-balancer --name my-alb --subnets subnet-123 subnet-456 --security-groups sg-123
aws elbv2 describe-load-balancers

# Target group (group of targets)
aws elbv2 create-target-group --name my-tg --protocol HTTP --port 80 --vpc-id vpc-123 --target-type instance
aws elbv2 register-targets --target-group-arn arn:aws:elasticloadbalancing:... --targets Id=i-123 Id=i-456

# Listener (traffic routing rule)
aws elbv2 create-listener --load-balancer-arn arn:aws:elasticloadbalancing:... --protocol HTTP --port 80 --default-actions Type=forward,TargetGroupArn=arn:aws:elasticloadbalancing:...

# Path-based rules (e.g., /api → another target group)
aws elbv2 create-rule --listener-arn arn:aws:elasticloadbalancing:... --priority 10 --conditions Field=path-pattern,Values=['/api/*'] --actions Type=forward,TargetGroupArn=...
```

### NAT Gateway

> **Provides outbound Internet access** to private subnets without exposing them.

- **What is it?** Managed network address translation (NAT) gateway for private subnets.
- **Price:** `$0.045/h` + `$0.045/GB` processed. The fixed cost is the surprise: ~`$32.85/month` per gateway!

```bash
aws ec2 create-nat-gateway --subnet-id subnet-123 --allocation-id eipalloc-123
aws ec2 describe-nat-gateways
aws ec2 delete-nat-gateway --nat-gateway-id nat-123
```

---

## Messaging and integration

### SQS

> **Simple Queue Service** — managed message queues (decouples services).

- **What is it?** Message queues with standard and FIFO models. The message is deleted when the consumer processes it (at-least-once).
- **Use cases:** decouple microservices, request buffers, asynchronous integrations, work fan-out.
- **Price:** `$0.40/1M` requests (the first million is free every month).

```bash
# Create a queue
aws sqs create-queue --queue-name my-queue
aws sqs list-queues
aws sqs get-queue-url --queue-name my-queue

# Send / receive / delete messages
aws sqs send-message --queue-url https://sqs.us-east-1.amazonaws.com/123456789012/my-queue --message-body "Hello world"
aws sqs send-message-batch --queue-url ... --entries '[{"Id":"1","MessageBody":"msg1"},{"Id":"2","MessageBody":"msg2"}]'
aws sqs receive-message --queue-url ... --max-number-of-messages 10 --wait-time-seconds 20
aws sqs delete-message --queue-url ... --receipt-handle "<ReceiptHandle>"
aws sqs purge-queue --queue-url ...   # empty ALL messages!

# Attributes and configuration
aws sqs get-queue-attributes --queue-url ... --attribute-names All
aws sqs set-queue-attributes --queue-url ... --attributes '{"VisibilityTimeout":"60","MessageRetentionPeriod":"345600"}'

# FIFO queues (guaranteed ordering)
aws sqs create-queue --queue-name my-queue.fifo --attributes '{"FifoQueue":"true","ContentBasedDeduplication":"true"}'
```

### SNS

> **Simple Notification Service** — publishes notifications to multiple subscribers (fan-out).

- **What is it?** Publishes messages to topics; subscribers (SQS, Lambda, email, SMS, HTTP) receive them.
- **Use cases:** alerts, email/SMS notifications, fan-out of events to multiple queues.
- **Price:** `$0.50/1M` publications (first million free). Email/SMS have additional costs.

```bash
# Create a topic
aws sns create-topic --name my-topic
aws sns list-topics

# Subscribe destinations
aws sns subscribe --topic-arn arn:aws:sns:us-east-1:123456789012:my-topic --protocol email --notification-endpoint me@mydomain.com
aws sns subscribe --topic-arn ... --protocol sqs --notification-endpoint arn:aws:sqs:us-east-1:123456789012:my-queue
aws sns subscribe --topic-arn ... --protocol lambda --notification-endpoint arn:aws:lambda:us-east-1:123456789012:function:my-function
aws sns list-subscriptions-by-topic --topic-arn ...

# Publish
aws sns publish --topic-arn arn:aws:sns:us-east-1:123456789012:my-topic --message "Alert: high CPU"
aws sns publish --topic-arn ... --message "{}" --message-structure json

# Delete
aws sns unsubscribe --subscription-arn arn:aws:sns:us-east-1:123456789012:subscription...
aws sns delete-topic --topic-arn ...
```

### EventBridge

> **Serverless event bus** — connects applications with AWS and third-party events.

- **What is it?** Receives events (`put-events`) and routes them through rules to targets (Lambda, SQS, SNS, Step Functions...).
- **Use cases:** event-driven architectures, reacting to infrastructure changes, SaaS integrations.
- **Price:** `$1.00/1M` custom events (AWS service events are free).

```bash
# Create a rule that detects an event
aws events put-rule --name "InstanceTerminated" --event-pattern '{"source":["aws.ec2"],"detail-type":["EC2 Instance State-change Notification"],"detail":{"state":["terminated"]}}'
aws events list-rules

# Add a target to the rule
aws events put-targets --rule InstanceTerminated --targets '[{"Id":"1","Arn":"arn:aws:lambda:us-east-1:123456789012:function:my-function"}]'

# Send an event manually
aws events put-events --entries '[{"Source":"my-app","DetailType":"UserCreated","Detail":"{\"userId\":\"123\"}","EventBusName":"default"}]'

# Delete
aws events remove-targets --rule InstanceTerminated --ids 1
aws events delete-rule --name InstanceTerminated
```

### Kinesis

> **Real-time data streaming** — ingests and processes continuous data.

- **What is it?** Kinesis Data Streams receives thousands of records per second and processes them with consumers (Lambda, KCL).
- **Use cases:** telemetry, real-time logs, clickstreams, IoT, real-time analytics.
- **Price:** `$0.015/shard-hour` + `$0.014/1M` records (PUT).

```bash
# Create a stream
aws kinesis create-stream --stream-name my-stream --shard-count 1
aws kinesis list-streams
aws kinesis describe-stream --stream-name my-stream

# Write records
aws kinesis put-record --stream-name my-stream --partition-key user-1 --data "$(echo '{"event":"click"}' | base64)"
aws kinesis put-records --stream-name my-stream --records file://records.json

# Read records
aws kinesis get-shard-iterator --stream-name my-stream --shard-id shardId-000000000000 --shard-iterator-type TRIM_HORIZON
aws kinesis get-records --shard-iterator <ShardIterator>

# Delete
aws kinesis delete-stream --stream-name my-stream
```

### Step Functions

> **Workflow orchestration** — coordinates multiple services in a visual workflow.

- **What is it?** State machine (ASL) that orchestrates Lambda, ECS, APIs, retries, parallelism, and human steps.
- **Use cases:** approval workflows, ETL processing, microservice orchestration, sagas.
- **Price:** `$0.025/1k` state transitions (Standard).

```bash
# Create a state machine
aws stepfunctions create-state-machine \
  --name my-flow \
  --definition '{"StartAt":"Hello","States":{"Hello":{"Type":"Pass","Result":"Hello","End":true}}}' \
  --role-arn arn:aws:iam::123456789012:role/sfn-role
aws stepfunctions list-state-machines

# Execute and monitor
aws stepfunctions start-execution --state-machine-arn arn:aws:states:us-east-1:123456789012:stateMachine:my-flow --input '{"key":"value"}'
aws stepfunctions list-executions --state-machine-arn arn:aws:states:...
aws stepfunctions describe-execution --execution-arn arn:aws:states:...
aws stepfunctions get-execution-history --execution-arn arn:aws:states:...

# Delete
aws stepfunctions delete-state-machine --state-machine-arn arn:aws:states:...
```

---

## Security, identity, and compliance

### IAM

> **Identity and Access Management** — manages users, roles, and permissions. **It is the most important AWS security service.**

- **What is it?** Controls who (identity) can do what (permission) on what (resource).
- **Concepts:** User (human user), Role (identity for services), Policy (JSON permissions document), Group.
- **Price:** **free** (100%).

```bash
# Users
aws iam create-user --user-name ana
aws iam list-users
aws iam create-access-key --user-name ana        # !!! save the keys
aws iam delete-user --user-name ana

# Roles
aws iam create-role --role-name lambda-role --assume-role-policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"lambda.amazonaws.com"},"Action":"sts:AssumeRole"}]}'
aws iam list-roles

# Policies
aws iam create-policy --policy-name s3-read-only --policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":["s3:GetObject","s3:ListBucket"],"Resource":"*"}]}'
aws iam list-policies --scope Local
aws iam attach-role-policy --role-name lambda-role --policy-arn arn:aws:iam::123456789012:policy/s3-read-only
aws iam attach-user-policy --user-name ana --policy-arn arn:aws:iam::aws:policy/AmazonS3ReadOnlyAccess

# View and validate permissions
aws iam get-account-summary
aws iam simulate-principal-policy --policy-source-arn arn:aws:iam::123456789012:user/ana --action-names s3:GetObject s3:DeleteBucket
aws iam get-policy-version --policy-arn ... --version-id v1
```

> **Principle of least privilege:** grant only the permissions needed. Each service in the project reads only its own parameters.

### AWS KMS

> **Key Management Service** — creates and manages encryption keys.

- **What is it?** Encryption key management service (CMK) used to encrypt data in S3, EBS, RDS, SSM, Secrets Manager, etc.
- **Use cases:** encryption at rest, secret encryption, data signing.
- **Price:** `$1.00/key/month` + `$0.03/10k` encryption/decryption requests.

```bash
# Create a key and an alias
aws kms create-key --description "Key for my-app"
aws kms create-alias --alias-name alias/my-app --target-key-id <KeyId>
aws kms list-keys
aws kms describe-key --key-id alias/my-app

# Encrypt and decrypt
aws kms encrypt --key-id alias/my-app --plaintext "$(echo -n 'secret' | base64)" --output text --query CiphertextBlob
aws kms decrypt --ciphertext-blob <CiphertextBlob> --output text --query Plaintext

# Rotation and deletion
aws kms enable-key-rotation --key-id alias/my-app
aws kms schedule-key-deletion --key-id <KeyId> --pending-window-in-days 7
```

### Secrets Manager

> **Secret management** — securely stores, rotates, and retrieves credentials and passwords.

- **What is it?** Stores secrets (passwords, API keys, connection strings) with built-in automatic rotation.
- **Use cases:** database credentials, API keys, tokens. Automatic rotation.
- **Price:** `$0.40/secret/month` + `$0.05/10k` API calls.

```bash
# Create a secret
aws secretsmanager create-secret --name prod/db/password --secret-string '{"username":"admin","password":"MySecurePassword!123"}'
aws secretsmanager create-secret --name prod/api-key --secret-string "sk-123456789"

# Read / list / update / rotate / delete
aws secretsmanager get-secret-value --secret-id prod/db/password
aws secretsmanager list-secrets
aws secretsmanager update-secret --secret-id prod/api-key --secret-string "sk-987654321"
aws secretsmanager rotate-secret --secret-id prod/db/password --rotation-rules '{"AutomaticallyAfterDays":30}'
aws secretsmanager delete-secret --secret-id prod/api-key --force-delete-without-recovery
```

### SSM Parameter Store

> **Parameter and configuration store** — centralized configuration and secrets at no cost.

- **What is it?** Parameter store (config) inside Systems Manager. Organized in hierarchical paths like `/app/env/service/key`.
- **Use cases:** connection strings, feature flags, centralized config, parameter versioning.
- **Price:** **Standard tier free** (no request limit). Advanced tier `$0.05/parameter/month`.

```bash
# Create / update parameters
aws ssm put-parameter --name "/myapp/dev/ConnectionStrings/MyDb" --value "Server=...;Database=..." --type String
aws ssm put-parameter --name "/myapp/prod/MessageBroker/Password" --value "secret" --type SecureString
aws ssm put-parameter --name "/myapp/dev/api/baseUrl" --value "https://api.dev" --type String --overwrite

# Read
aws ssm get-parameter --name "/myapp/dev/api/baseUrl"
aws ssm get-parameter --name "/myapp/prod/MessageBroker/Password" --with-decryption
aws ssm get-parameters --names "/myapp/dev/api/baseUrl" "/myapp/dev/api/key" --with-decryption
aws ssm get-parameters-by-path --path "/myapp/dev/" --recursive --with-decryption

# List / history / delete
aws ssm describe-parameters
aws ssm get-parameter-history --name "/myapp/prod/MessageBroker/Password"
aws ssm delete-parameter --name "/myapp/dev/api/baseUrl"
aws ssm delete-parameters --names "/myapp/dev/api/baseUrl" "/myapp/dev/api/key"
```

### Amazon Cognito

> **Authentication and identity for applications** — sign-up, sign-in, and access control.

- **What is it?** User Pools (user registration/login) + Identity Pools (temporary AWS credentials). Includes federation with Google, Facebook, etc.
- **Use cases:** user authentication in mobile/web apps, OAuth2/OIDC, MFA.
- **Price:** first **50,000 active users/month free**; then ~`$0.0055/MAU`.

```bash
# User Pool (user database)
aws cognito-idp create-user-pool --pool-name my-pool
aws cognito-idp create-user-pool-client --user-pool-id <PoolId> --client-name web-app --no-generate-secret
aws cognito-idp list-user-pools --max-results 10

# Users
aws cognito-idp admin-create-user --user-pool-id <PoolId> --username ana@mail.com --temporary-password "Temp!123"
aws cognito-idp admin-set-user-password --user-pool-id <PoolId> --username ana@mail.com --password "MySecurePassword!123" --permanent
aws cognito-idp list-users --user-pool-id <PoolId>

# Identity Pool (AWS credentials)
aws cognito-identity create-identity-pool --identity-pool-name my-identity --allow-unauthenticated-identities
aws cognito-identity list-identity-pools --max-results 10
```

### AWS WAF

> **Web Application Firewall** — protects your web applications against attacks (SQLi, XSS, DDoS).

- **What is it?** Application-level firewall that filters traffic before it reaches your app (CloudFront, ALB, API Gateway).
- **Use cases:** block malicious IPs, mitigate SQL injection/XSS, rate limiting, managed rules.
- **Price:** `$5.00/Web ACL/month` + `$1.00/1M` requests. Extra for managed rules.

```bash
# Create a Web ACL
aws wafv2 create-web-acl \
  --name my-acl --scope CLOUDFRONT --default-action '{"Allow":{}}' \
  --visibility-config '{"SampledRequestsEnabled":true,"CloudWatchMetricsEnabled":true,"MetricName":"my-acl"}' \
  --region us-east-1
aws wafv2 list-web-acls --scope CLOUDFRONT --region us-east-1

# IP Set (list of allowed/blocked IPs)
aws wafv2 create-ip-set --name bad-ips --scope REGIONAL --ip-address-version IPV4 --addresses "1.2.3.4/32" --region us-east-1
aws wafv2 list-ip-sets --scope REGIONAL --region us-east-1

# Associate the ACL with a resource (ALB, CloudFront, API Gateway)
aws wafv2 associate-web-acl --web-acl-arn arn:aws:wafv2:... --resource-arn arn:aws:elasticloadbalancing:...

# Delete
aws wafv2 delete-web-acl --name my-acl --scope REGIONAL --id <Id> --lock-token <LockToken> --region us-east-1
```

---

## Management and governance

### CloudWatch

> **Monitoring and observability** — metrics, logs, and alarms for everything running on AWS.

- **What is it?** Collects metrics (CPU, memory, latency...), logs, and generates alarms/notifications.
- **Use cases:** dashboards, alerts, application monitoring, metric-based auto scaling.
- **Price:** metrics `$0.30/metric/month`; Logs `$0.50/GB` ingested + `$0.03/GB` stored; alarms `$0.10/alarm/month`.

```bash
# Metrics
aws cloudwatch put-metric-data --namespace "MyApp" --metric-name "Orders" --value 42 --unit Count
aws cloudwatch list-metrics --namespace "MyApp"
aws cloudwatch get-metric-statistics --namespace "MyApp" --metric-name "Orders" --dimensions Name=Service,Value=Api --start-time 2026-07-01T00:00:00Z --end-time 2026-08-01T00:00:00Z --period 3600 --statistics Average

# Alarms
aws cloudwatch put-metric-alarm \
  --alarm-name high-cpu --alarm-description "CPU > 80%" \
  --metric-name CPUUtilization --namespace AWS/EC2 --statistic Average \
  --period 300 --threshold 80 --comparison-operator GreaterThanThreshold \
  --evaluation-periods 2 --dimensions Name=InstanceId,Value=i-123 \
  --alarm-actions arn:aws:sns:us-east-1:123456789012:my-topic
aws cloudwatch describe-alarms

# Logs
aws logs create-log-group --log-group-name /my-app/prod
aws logs create-log-stream --log-group-name /my-app/prod --log-stream-name api-1
aws logs put-log-events --log-group-name /my-app/prod --log-stream-name api-1 --log-events '[{"timestamp":1750000000000,"message":"request ok"}]'
aws logs filter-log-events --log-group-name /my-app/prod --filter-pattern "ERROR"
aws logs describe-log-groups
aws logs tail /my-app/prod --follow
```

### CloudTrail

> **API audit** — records every AWS API call (who did what and when).

- **What is it?** Immutable record of user and service actions in your account (governance, compliance, forensics).
- **Price:** **management events are free** (90-day retention). Data events `$0.10/100k` events.

```bash
# Create a trail (send to S3 / CloudWatch Logs)
aws cloudtrail create-trail --name my-trail --s3-bucket-name my-bucket-cloudtrail --is-multi-region-trail
aws cloudtrail start-logging --name my-trail

# Search events
aws cloudtrail lookup-events --lookup-attributes AttributeKey=EventName,AttributeValue=TerminateInstances
aws cloudtrail lookup-events --lookup-attributes AttributeKey=Username,AttributeValue=ana --start-time 2026-07-01T00:00:00Z

# Status
aws cloudtrail describe-trails
aws cloudtrail get-trail-status --name my-trail
aws cloudtrail stop-logging --name my-trail
```

### CloudFormation

> **Infrastructure as Code (IaC)** — defines your entire infrastructure in YAML/JSON templates and deploys it.

- **What is it?** You declare resources (EC2, S3, RDS...) in a template and CloudFormation creates/updates/deletes them in order.
- **Use cases:** reproduce environments, version infrastructure, repeatable deployments.
- **Price:** **free** (you only pay for the resources it creates).

```bash
# Create a stack from a template
aws cloudformation create-stack --stack-name my-stack --template-body file://template.yaml --capabilities CAPABILITY_NAMED_IAM
aws cloudformation create-stack --stack-name my-stack --template-url https://s3.amazonaws.com/my-bucket/template.yaml

# Status and events
aws cloudformation describe-stacks --stack-name my-stack
aws cloudformation list-stacks --stack-status-filter CREATE_COMPLETE
aws cloudformation describe-stack-events --stack-name my-stack

# Update / delete
aws cloudformation update-stack --stack-name my-stack --template-body file://template.yaml --capabilities CAPABILITY_NAMED_IAM
aws cloudformation delete-stack --stack-name my-stack

# Validate a template without deploying
aws cloudformation validate-template --template-body file://template.yaml

# Change Sets (see what will change before applying it)
aws cloudformation create-change-set --stack-name my-stack --template-body file://template.yaml --change-set-name changes --capabilities CAPABILITY_NAMED_IAM
aws cloudformation execute-change-set --change-set-name changes --stack-name my-stack
```

### AWS Config

> **Compliance and configuration** — continuously audits the configuration of your resources.

- **What is it?** Evaluates resources against rules (are buckets public? is encryption enabled?) and keeps a history.
- **Price:** `$0.003/configuration item/month` + rules per evaluation.

```bash
aws configservice describe-configuration-recorders
aws configservice put-config-rule --config-rule '{"ConfigRuleName":"s3-public","Source":{"Owner":"AWS","SourceIdentifier":"S3_BUCKET_PUBLIC_READ_PROHIBITED"}}'
aws configservice describe-config-rules
aws configservice describe-compliance-by-config-rule
aws configservice get-compliance-details-by-config-rule --config-rule-name s3-public
```

### AWS Budgets

> **Budgets and cost alerts** — no surprises on your bill.

- **What is it?** Defines monthly budgets and email alerts when costs approach the limit.
- **Price:** 2 free budgets; then `$0.02/budget/day`.

```bash
aws budgets create-budget \
  --account-id 123456789012 \
  --budget '{"BudgetName":"My-budget","BudgetLimit":{"Amount":"100","Unit":"USD"},"TimeUnit":"MONTHLY","BudgetType":"COST"}' \
  --notifications-with-subscribers '[{"Notification":{"NotificationType":"ACTUAL","ComparisonOperator":"GREATER_THAN","Threshold":80},"Subscribers":[{"SubscriptionType":"EMAIL","Address":"me@mydomain.com"}]}]'
aws budgets describe-budgets --account-id 123456789012
```

---

## Analytics

### Athena

> **Serverless SQL over S3** — queries files directly without loading them into a database.

- **What is it?** Runs SQL directly on data in S3 (CSV, JSON, Parquet, Glue Catalog). No servers.
- **Use cases:** ad-hoc analysis, log queries, data lakes.
- **Price:** `$5.00/TB` scanned.

```bash
# Run a query
aws athena start-query-execution \
  --query-string "SELECT name, COUNT(*) FROM \"my-bucket\".\"my-table\" GROUP BY name" \
  --result-configuration '{"OutputLocation":"s3://athena-results/"}'
aws athena get-query-execution --query-execution-id <QueryExecutionId>
aws athena get-query-results --query-execution-id <QueryExecutionId>

# Workgroups (organization and cost control)
aws athena create-work-group --name analytics --description "Analytics workgroup"
aws athena list-work-groups
```

### AWS Glue

> **Serverless ETL** — prepares and transforms data for analytics.

- **What is it?** Data integration service: crawlers that catalog data in S3, and ETL jobs (Python/Scala/Spark).
- **Use cases:** build data lakes, clean/transform data, table catalog for Athena.
- **Price:** `$0.44/DPU-hour` (jobs) — the catalog and crawlers have their own pricing.

```bash
# Catalog: databases and tables
aws glue create-database --database-input '{"Name":"my-db"}'
aws glue get-databases
aws glue get-tables --database-name my-db

# Crawlers (auto-discover schemas)
aws glue create-crawler --name my-crawler --role arn:aws:iam::123456789012:role/glue-role --targets '{"S3Targets":[{"Path":"s3://my-bucket/data"}]}' --database-name my-db
aws glue start-crawler --name my-crawler
aws glue get-crawler --name my-crawler

# ETL jobs
aws glue create-job --name my-job --role arn:aws:iam::123456789012:role/glue-role --command '{"Name":"pythonshell","PythonVersion":"3","ScriptLocation":"s3://my-bucket/scripts/etl.py"}' --max-capacity 2
aws glue start-job-run --job-name my-job
aws glue get-job-runs --job-name my-job
```

### Amazon EMR

> **Big data** — managed Hadoop, Spark, Hive, Presto clusters.

- **What is it?** Launches distributed computing clusters to process large volumes of data.
- **Use cases:** massive Spark processing, big data ETL, distributed machine learning.
- **Price:** underlying EC2 + per-node surcharge (~30%).

```bash
# Create a cluster
aws emr create-cluster \
  --name my-cluster \
  --release-label emr-7.1.0 \
  --applications Name=Spark \
  --instance-groups '[{"InstanceCount":2,"InstanceGroupType":"CORE","InstanceType":"m5.xlarge"}]' \
  --ec2-attributes '{"KeyName":"my-key","InstanceProfile":"EMR_EC2_DefaultRole"}' \
  --service-role EMR_DefaultRole
aws emr list-clusters

# Add steps (Spark jobs)
aws emr add-steps --cluster-id j-123 --steps '[{"Name":"my-step","Jar":"command-runner.jar","Args":["spark-submit","s3://my-bucket/scripts/job.py"],"ActionOnFailure":"CONTINUE"}]'

# Status and termination
aws emr describe-cluster --cluster-id j-123
aws emr terminate-job-flows --cluster-ids j-123
```

---

## Machine Learning and AI

### Amazon SageMaker

> **Complete ML platform** — build, train, and deploy machine learning models.

- **What is it?** Managed environment for the full ML lifecycle: notebooks, distributed training, endpoint deployment.
- **Use cases:** prediction models, classification, recommendation, NLP and vision.
- **Price:** from ~`$0.05/h` (notebook `ml.t3.medium`) to hundreds of $/h for training GPUs.

```bash
# Notebook instance
aws sagemaker create-notebook-instance --notebook-instance-name my-notebook --instance-type ml.t3.medium --role-arn arn:aws:iam::123456789012:role/sagemaker-role

# Training
aws sagemaker create-training-job --training-job-name my-training --algorithm-specification '{"TrainingImage":"...","TrainingInputMode":"File"}' --role-arn arn:aws:iam::123456789012:role/sagemaker-role --resource-config '{"InstanceType":"ml.m5.large","InstanceCount":1}'
aws sagemaker list-training-jobs

# Deploy a model
aws sagemaker create-model --model-name my-model --primary-container '{"Image":"...","ModelDataUrl":"s3://my-bucket/model/model.tar.gz"}'
aws sagemaker create-endpoint-config --endpoint-config-name my-config --production-variants '[{"VariantName":"v1","ModelName":"my-model","InitialInstanceCount":1,"InstanceType":"ml.m5.large"}]'
aws sagemaker create-endpoint --endpoint-name my-endpoint --endpoint-config-name my-config
aws sagemaker describe-endpoint --endpoint-name my-endpoint
```

### Amazon Bedrock

> **Generative AI models as an API** — access Claude, Llama, Mistral, Titan, Nova, etc. without managing infrastructure.

- **What is it?** Single API to use foundation models (FMs) from different providers (Anthropic, Meta, Mistral, AI21, Cohere, Amazon).
- **Use cases:** chatbots, text generation, summarization, agents, RAG.
- **Price:** per-token/per-use for each model (varies by model; some have a free tier).

```bash
# List available foundation models
aws bedrock list-foundation-models --output table

# Invoke a model (Claude)
aws bedrock-runtime invoke-model \
  --model-id anthropic.claude-3-5-sonnet-20241022-v2:0 \
  --body '{"anthropic_version":"bedrock-2023-05-31","max_tokens":512,"messages":[{"role":"user","content":"Explain AWS in one sentence"}]}' \
  --region us-east-1 out.json

# Invoke with streaming (real-time response)
aws bedrock-runtime invoke-model-with-response-stream --model-id anthropic.claude-3-5-sonnet-... --body '{"anthropic_version":"bedrock-2023-05-31","max_tokens":128,"messages":[{"role":"user","content":"Hello"}]}'

# Converse API (unified interface for all models)
aws bedrock-runtime converse \
  --model-id amazon.nova-micro-v1:0 \
  --messages '[{"role":"user","content":[{"text":"What is 2+2?"}]}]'
```

---

## Development and DevOps tools

### CodeCommit / CodeBuild / CodePipeline / CodeDeploy

> **AWS native CI/CD suite** — git repositories, builds, pipelines, and deployments.

| Service | Function | Price |
|----------|----------|-------|
| **CodeCommit** | Private Git repositories | Free (5 users, 50 GB) |
| **CodeBuild** | Serverless container builds | Per build minute (`$0.005/min` Linux) |
| **CodePipeline** | CI/CD orchestration (source → build → deploy) | `$1.00/pipeline/month` (active) |
| **CodeDeploy** | Automated deployments (EC2, ECS, Lambda, on-premises) | Free |

```bash
# CodeCommit
aws codecommit create-repository --repository-name my-repo
aws codecommit list-repositories
git push https://git-codecommit.us-east-1.amazonaws.com/v1/repos/my-repo

# CodeBuild
aws codebuild create-project --name my-build --source '{"Type":"CODECOMMIT","Location":"..."}' --environment '{"Type":"LINUX_CONTAINER","ComputeType":"BUILD_GENERAL1_SMALL","Image":"aws/codebuild/amazonlinux2-x86_64-standard:5.0"}' --service-role arn:aws:iam::123456789012:role/codebuild-role
aws codebuild start-build --project-name my-build
aws codebuild batch-get-builds --ids <BuildId>

# CodePipeline
aws codepipeline create-pipeline --cli-input-json file://pipeline.json
aws codepipeline list-pipelines
aws codepipeline start-pipeline-execution --name my-pipeline

# CodeDeploy
aws deploy create-application --application-name my-app
aws deploy create-deployment --application-name my-app --deployment-group-name my-group --s3-location bucket=my-bucket,key=app.zip,bundleType=zip
```

---

## Services used in this project

This educational project uses **one** of the 200+ AWS services:

| AWS service | Use in the project | Local alternative |
|-------------|--------------------|-------------------|
| **SSM Parameter Store** | Centralized configuration (connection strings, message broker, OpenTelemetry) for the API, OutboxProcessor, and Consumer | **MiniStack** (emulator at `localhost:4566`) |

- **SSM Parameter Store** is used to read configuration at runtime, overriding the `appsettings.json` files (which remain as fallback).
- The integration is implemented in .NET with `Amazon.Extensions.Configuration.SystemsManager`.
- In **local development** the parameters come from MiniStack; in **production** from real AWS (IAM Role + `SecureString`).

For implementation details and how to emulate AWS locally:

| Topic | Document |
|-------|----------|
| **MiniStack (local emulator)** | [`README.Ministack.en.md`](README.Ministack.en.md) |
| **SSM integration in .NET** | [`README.Ministack.en.md`](README.Ministack.en.md) |
| **Main project README** | [`README.en.md`](README.en.md) |

---

## References

- [AWS — Official website](https://aws.amazon.com/)
- [AWS Cloud Computing](https://aws.amazon.com/what-is-aws/)
- [AWS Pricing](https://aws.amazon.com/pricing/)
- [AWS Pricing Calculator](https://calculator.aws/)
- [AWS Free Tier](https://aws.amazon.com/free/)
- [AWS CLI Command Reference](https://awscli.amazonaws.com/v2/documentation/api/latest/index.html)
- [AWS Services — Full list](https://aws.amazon.com/products/)
- [AWS Global Infrastructure](https://aws.amazon.com/about-aws/global-infrastructure/)
- [SSM Parameter Store documentation](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html)
- [IAM Best Practices](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html)
- [AWS Architecture Center](https://aws.amazon.com/architecture/)

---

**Happy learning!** 🚀
