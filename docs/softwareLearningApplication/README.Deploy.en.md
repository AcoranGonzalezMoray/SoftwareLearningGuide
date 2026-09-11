<h1 align="center">Deployment — Infrastructure as Code + Helm</h1>

<p align="center">
  <em>Complete guide for deploying the three project services and the full flow <strong>API → OutboxProcessor → Consumer</strong>. Two scenarios: local emulation with MiniStack and real deployment in AWS</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Terraform-7B42BC?style=for-the-badge&logo=terraform&logoColor=white" alt="Terraform">
  <img src="https://img.shields.io/badge/Helm-0F1689?style=for-the-badge&logo=helm&logoColor=white" alt="Helm">
  <img src="https://img.shields.io/badge/AWS-FF9900?style=for-the-badge&logo=amazon-aws&logoColor=white" alt="AWS">
  <img src="https://img.shields.io/badge/MiniStack-45B7D1?style=for-the-badge&logo=docker&logoColor=white" alt="MiniStack">
  <img src="https://img.shields.io/badge/Kubernetes-326CE5?style=for-the-badge&logo=kubernetes&logoColor=white" alt="Kubernetes">
</p>

---

## Table of Contents

1. [Deployment Architecture](#deployment-architecture)
    - [Components](#components)
    - [Folder Structure](#folder-structure)
2. [Prerequisites](#prerequisites)
3. [Local Emulation Flow (MiniStack)](#local-emulation-flow-ministack)
    - [Step 0 — Start Supporting Infrastructure](#step-0--start-supporting-infrastructure)
    - [Step 1 — AWS CLI Pointing to MiniStack](#step-1--aws-cli-pointing-to-ministack)
    - [Step 2 — Run the Apps Locally](#step-2--run-the-apps-locally)
    - [Step 3 — Test the Complete Flow](#step-3--test-the-complete-flow)
    - [Step 4 — Observability](#step-4--observability)
    - [Step 5 — Emulated Flow: Terraform + Helm against MiniStack](#step-5--emulated-flow-terraform--helm-against-ministack)
    - [Step 6 — Operate MiniStack with AWS CLI](#step-6--operate-ministack-with-aws-cli)
    - [Step 7 — Teardown (Local Emulation)](#step-7--teardown-local-emulation)
4. [Terraform: Without Credentials, With Credentials, or against MiniStack](#terraform-sin-credenciales-con-credenciales-o-contra-ministack)
5. [Real Deployment in AWS (Reference)](#real-deployment-in-aws-reference)
6. [Daily Operations (AWS Real)](#daily-operations-aws-real)
7. [Troubleshooting](#troubleshooting)
8. [Security and Best Practices](#security-and-best-practices)
9. [Related Documentation](#related-documentation)

---

## Deployment Architecture

```
 Emulation local (no real AWS)                         AWS real (reference)
 -------------------------------                       -----------------------
                                             ┌──────────────────────────────────────┐
┌───────────────────────────────┐            │  AWS: VPC + EKS                      │
│ YOUR MACHINE                  │            │                                      │
│ ┌────────────┐ ┌─────────────┐│            │  ┌───────────────┐                   │
│ │  API       │ │ Outbox +    ││            │  │ software-guide│                   │
│ │  (dotnet)  │ │ Consumer    ││            │  │ -api / outbox │                   │
│ └─────┬──────┘ │ (dotnet)    ││            │  │ / consumer    │  Deployments      │
│       │        └──────┬──────┘│            │  └───┬─────┬─────┘                   │
│       │                │      │            │      │     │                         │
│   Docker Compose       │      │            │   RDS / AmazonMQ / SNS/SQS / Cognito │
│  ┌─────────────────────┴────┐ │            │  (managed AWS services)              │
│  │ SQL ◄─ RabbitMQ ◄─ Redis │ │            └──────────────────────────────────────┘
│  │ Flagsmith ◄─ MiniStack   │ │
│  │ (SSM/SNS/SQS/Cognito)    │ │
│  └──────────────────────────┘ │
└───────────────────────────────┘

Configuration:  local       → appsettings.Development.json (already points to localhost and MiniStack)
                Kubernetes → ConfigMap → /app/appsettings.Production.json (one chart per service)
K8s Install:  helm upgrade --install <release> deploy/helm/<chart>
```

### Components

| Component | Chart / Terraform Module | Role |
|------------|--------------------------|------|
| **REST API** | `helm/software-guide-api` | Exposes endpoints, persists in SQL Server, writes the Outbox table in the same transaction |
| **OutboxProcessor** | `helm/outbox-processor` | Worker that scans the `OutboxMessage` table and publishes Integration Events to RabbitMQ (and SNS/SQS if AWS is enabled) |
| **Consumer** | `helm/consumer` | Worker that consumes Integration Events from RabbitMQ (and the SNS/SQS bus) and executes business logic |
| **Network (VPC)** | `IaC/modules/vpc` | Multi-AZ VPC, public/private subnets, NAT, Internet Gateway, Flow Logs |
| **Images (ECR)** | `IaC/modules/ecr` | One repository per image, with lifecycle policy and scanning |
| **Kubernetes (EKS)** | `IaC/modules/eks` | EKS cluster, managed node group, add-ons, Access Entries, OIDC (IRSA) |

### Folder Structure

```
deploy/
├── README.Deploy.md                   ← this guide
├── IaC/                                # Terraform (VPC, ECR, EKS)
│   ├── main.tf                         # Providers + module orchestration
│   ├── variables.tf                    # All parameterized variables
│   ├── outputs.tf                      # kubeconfig, ECR URLs, network IDs, OIDC...
│   ├── versions.tf                     # Terraform/provider versions + backend
│   ├── terraform.tfvars.example        # Example values (copy to terraform.tfvars)
│   ├── terraform.minstack.tfvars.example # Values for MiniStack emulation
│   └── modules/
│       ├── vpc/     (main/variables/outputs)   # VPC, subnets, NAT, Flow Logs
│       ├── ecr/     (main/variables/outputs)   # ECR repo + lifecycle policy
│       └── eks/     (main/variables/outputs)   # Cluster, node group, add-ons, OIDC
├── helm/
│   ├── software-guide-api/             # API chart (Deployment + Service)
│   │   ├── Chart.yaml
│   │   ├── values.yaml                 # image, replicas, resources, full config
│   │   └── templates/
│   │       ├── _helpers.tpl            # Reusable name/label helpers
│   │       ├── configmap.yaml          # appsettings.Production.json from values
│   │       ├── deployment.yaml         # Deployment + probes + resources + env
│   │       ├── service.yaml            # Service (ClusterIP / NodePort)
│   │       └── NOTES.txt               # post-installation instructions
│   ├── outbox-processor/               # Worker chart (Deployment only)
│   │   ├── Chart.yaml
│   │   ├── values.yaml
│   │   └── templates/ (configmap/deployment/helpers/NOTES)
│   └── consumer/                       # Worker chart (Deployment only)
│       ├── Chart.yaml
│       ├── values.yaml
│       └── templates/ (configmap/deployment/helpers/NOTES)
└── scripts/
    └── ministack.ps1                   # AWS CLI → MiniStack: profile, verify, reseed SSM
```

The **Dockerfiles** for each service live in the project folder:
`SoftwareLearningGuide.Api/Dockerfile`, `SoftwareLearningGuide.OutboxProcessor/Dockerfile`
(existing), `SoftwareLearningGuide.Consumer/Dockerfile`.

---

## Prerequisites

| Tool | Minimum Version | Installation (Windows) |
|------|-----------------|------------------------|
| Docker Desktop | 4.x | `winget install Docker.DockerDesktop` (enable engine in WSL 2) |
| .NET SDK | 10.x | `winget install Microsoft.DotNet.SDK.10` |
| AWS CLI | 2.x | `winget install Amazon.AWSCLI` (to operate MiniStack) |
| Terraform | 1.5+ | `winget install Hashicorp.Terraform` (IaC only) |

**For Kubernetes cluster deployment** (the "full emulated" option against MiniStack's EKS and real AWS deployment): Helm 3.11+ (`winget install Helm.Helm`) and kubectl 1.28+ (`winget install Kubernetes.kubectl`). The quick flow with `dotnet run` doesn't use these.

Quick verification:

```powershell
docker --version
dotnet --version
aws --version
```

> **Local emulation = MiniStack, not minikube.** The supporting infrastructure (SQL, RabbitMQ, Redis, Flagsmith, Aspire, and the AWS emulator) runs in containers **on the host** (`localhost`), and the three apps run with `dotnet run`. The AWS CLI is used to *operate* MiniStack (SSM/SNS/SQS/Cognito), never against real AWS.

---

## Local Emulation Flow (MiniStack)

> **Goal: run the complete system at no cost and without real AWS.** In `Development` mode, all three apps **already enable MiniStack by default**: the development configuration points to `http://localhost:4566` for SSM/Messaging/Cognito, to `localhost` for SQL/RabbitMQ, and to `http://localhost:4317` for OTLP.

### Step 0 — Start Supporting Infrastructure (MiniStack Included)

From the **repository root**:

```powershell
docker-compose up -d
```

This starts (host ports):

| Service | Port | Usage |
|----------|------|-------|
| SQL Server | `1433` | Main database (API + OutboxProcessor) |
| RabbitMQ | `5672` / `15672` | Message broker (management: `guest/guest`) |
| MiniStack | `4566` | AWS emulator (SSM, SNS, SQS, Cognito) |
| Redis | `6379` | MiniStack state backend |
| Flagsmith | `8000` | Feature management (used by API in dev) |
| Aspire Dashboard | `18888` / `4317` | Observability OTLP |

MiniStack **seeds SSM and Cognito on startup** (scripts `scripts/ministack/init-ssm.sh` and `cognito-init.sh` are mounted at `/docker-entrypoint-initaws.d/ready.d`): SSM parameters with `localhost` values and a Cognito user pool with `admin@test.com` / `Test1234!` (group `admin`) and `user@test.com` / `Test1234!` (group `normal`).

```powershell
docker-compose ps
```

### Step 1 — AWS CLI Pointing to MiniStack

**Yes: you need the AWS CLI pointing to the MiniStack endpoint.** MiniStack accepts any credentials; by convention `test`/`test` are used in `us-east-1`.

There's a helper that does everything (profile, verification, and re-seed):

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action All
```

Equivalent to (manually):

```powershell
# 1) AWS CLI profile pointing to MiniStack
aws configure set aws_access_key_id     test        --profile ministack
aws configure set aws_secret_access_key test        --profile ministack
aws configure set region                us-east-1   --profile ministack
aws configure set endpoint_url          http://localhost:4566 --profile ministack   # AWS CLI v2

# 2) From now on, ALWAYS with --profile ministack --endpoint-url http://localhost:4566
aws --profile ministack --endpoint-url http://localhost:4566 ssm get-parameters-by-path `
  --path "/softwarelearningguide/" --recursive --query "Parameters[].{Name:Name,Value:Value}" `
  --output table
```

> With AWS CLI v1, `endpoint_url` doesn't exist per profile: pass `--endpoint-url` on each command (the helper does it for you). The `test`/`test` credentials only work against MiniStack.

Verify the seed responded (SSM, Cognito, SNS/SQS):

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Verify
```

If something was left empty or you restarted MiniStack without persistence:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Reseed
```

> **Cognito** parameters (`UserPoolId`/`ClientId`) are dynamic and generated by `cognito-init.sh`. If your pool disappeared, re-run it inside the container: `docker compose exec ministack sh /docker-entrypoint-initaws.d/ready.d/cognito-init.sh`

### Step 2 — Run the Apps Locally

Start the three projects in `Development` mode (their configuration already uses **SQL + RabbitMQ + MiniStack + OTLP** on localhost). From the **repo root**:

```powershell
dotnet run --project SoftwareLearningGuide.Api --launch-profile http
dotnet run --project SoftwareLearningGuide.OutboxProcessor --environment Development
dotnet run --project SoftwareLearningGuide.Consumer --environment Development
```

> The `--environment Development` (or `$env:ASPNETCORE_ENVIRONMENT="Development"`) makes the workers load their `appsettings.Development.json`, which already points to SQL/RabbitMQ/MiniStack on `localhost`.

> In Visual Studio you can launch all three at once with the **"Api + Outbox + Consumer"** startup profile (`SoftwareLearningGuide.slnLaunch`).

The API starts at `http://localhost:5089` (Swagger at `/swagger`). Workers write to their console and publish telemetry to the Aspire Dashboard.

### Step 3 — Test the Complete Flow

1) With **Cognito active** (dev mode) endpoints require JWT. Use the API help utility with your username/password to get the `access_token`:

```powershell
$TOKEN = (curl.exe -s -X POST http://localhost:5089/api/v1/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "username=admin@test.com&password=Test1234!" | ConvertFrom-Json).access_token
```

> You can also get it with the AWS CLI pointing to MiniStack (see Step 6).

2) Create a product (this writes `Product` + `OutboxMessage` in the **same transaction**):

```powershell
curl.exe -s -X POST http://localhost:5089/api/v1/product `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer $TOKEN" `
  -d '{"name":"Laptop XP","description":"Gaming laptop","price":1200.00,"currency":"USD","stockQuantity":10}'
```

3) The **OutboxProcessor** detects the pending message (polls every ~5s) and publishes to RabbitMQ. Check **its console** and you should see the `ProductCreatedIntegrationEvent` publication.

4) The **Consumer** receives the event from RabbitMQ (and its SNS/SQS bus from MiniStack) and executes the logic. Check **its console**:

```
[ProductCreatedConsumer] Recibido ProductCreatedIntegrationEvent
  ProductId=... | Name=Laptop XP | Price=1200.00 | Currency=USD
```

5) Verify RabbitMQ exchanges/queues in the UI: `http://localhost:15672` (`guest` / `guest`) → *Exchanges* / *Queues*.

> If an endpoint returns `404`, check that the corresponding feature flag `FT_*` is enabled in Flagsmith (`http://localhost:8000`); in dev the API reads its flags from local Flagsmith.

### Step 4 — Observability (optional)

Without changing anything: the dev apps send OTLP to `http://localhost:4317` (the Aspire Dashboard port). Open `http://localhost:18888` and you'll see traces, metrics, and logs from all three processes.

### Step 5 — Emulated Flow: Terraform + Helm against MiniStack (optional)

> Up to here the apps run with `dotnet run`. This option reproduces the **real production deployment** but against MiniStack: **Terraform creates VPC/ECR/EKS in the emulator** (its EKS spins up a real **k3s/k3d cluster embedded in Docker**) and **Helm installs the charts on that cluster**. Nothing touches real AWS.

1) Create the emulated infrastructure (local state; MiniStack must be running):

```powershell
cd deploy/IaC
terraform init
terraform plan  -var-file="terraform.minstack.tfvars.example"
terraform apply -var-file="terraform.minstack.tfvars.example"
```

> With this tfvars the provider points **ALL** services to `http://localhost:4566` (`test`/`test` credentials, no STS calls) and disables parts the emulator doesn't support: NAT, flow logs, node group, add-ons, Access Entries, and OIDC. The first time, MiniStack **downloads and starts k3d/k3s** when creating the cluster (may take a couple of minutes; verify with `docker compose logs ministack`).

Verify the emulated resources:

```powershell
aws --profile ministack --endpoint-url http://localhost:4566 ecr describe-repositories
aws --profile ministack --endpoint-url http://localhost:4566 eks describe-cluster --name softwarelearningguide-dev-eks
```

2) Point `kubectl` to the emulated cluster (MiniStack's k3s), re-seed SSM and patch CoreDNS so pods can resolve `host.docker.internal`. `Prepare` does all three (idempotent; run it **every time after** `terraform apply`):

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Prepare -AppHost host.docker.internal
kubectl get nodes
```

> `aws eks update-kubeconfig` generates a context using `aws eks get-token`, but **MiniStack's k3s doesn't validate that token**: the `Kubeconfig` helper extracts the **admin** kubeconfig (`/etc/rancher/k3s/k3s.yaml`) from the k3s container and installs it in the `slg-minstack` context of your `~/.kube/config`. You should see 1 node `Ready`. From here `kubectl`/`helm` operate as if it were EKS.
>
> The CoreDNS patch is mandatory: pods resolve `host.docker.internal` through CoreDNS `NodeHosts` (`192.168.65.254`); without it they fail with *host not found* when connecting to SQL/RabbitMQ/MiniStack.

3) Build and push images to the **emulated ECR** of MiniStack. The registry URI is returned by Terraform (`terraform output ecr_repository_urls`); typically it's `localhost:4566`:

```powershell
docker build -f SoftwareLearningGuide.Api/Dockerfile              -t softwarelearningguide/software-guide-api:local  .
docker build -f SoftwareLearningGuide.OutboxProcessor/Dockerfile  -t softwarelearningguide/outbox-processor:local    .
docker build -f SoftwareLearningGuide.Consumer/Dockerfile         -t softwarelearningguide/consumer:local            .

aws --profile ministack --endpoint-url http://localhost:4566 ecr get-login-password `
  | docker login --username AWS --password-stdin localhost:4566

docker tag softwarelearningguide/software-guide-api:local   localhost:4566/softwarelearningguide-api:latest
docker tag softwarelearningguide/outbox-processor:local     localhost:4566/softwarelearningguide-outboxprocessor:latest
docker tag softwarelearningguide/consumer:local             localhost:4566/softwarelearningguide-consumer:latest

docker push localhost:4566/softwarelearningguide-api:latest
docker push localhost:4566/softwarelearningguide-outboxprocessor:latest
docker push localhost:4566/softwarelearningguide-consumer:latest
```

> Alternative to not using the registry: import images directly into the k3s node: `docker ps | Select-String k3s` to find the container, then `docker cp <image> <cluster-node>:/...` or `k3d image import <image> -c <cluster>`.

4) Install the charts. **`image.repository` must be the mirror host pre-configured by the k3s** (`000000000000.dkr.ecr.us-east-1.amazonaws.com`): each `values.yaml` already defaults to it, and `values-emulated.yaml` sets the config that points to `host.docker.internal`. Do NOT use `host.docker.internal:4566` as `image.repository` (no mirror → the kubelet tries HTTPS → `ErrImagePull`):

```powershell
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg --create-namespace `
  -f ./deploy/helm/software-guide-api/values-emulated.yaml

helm upgrade --install outbox-processor ./deploy/helm/outbox-processor -n slg `
  -f ./deploy/helm/outbox-processor/values-emulated.yaml

helm upgrade --install consumer ./deploy/helm/consumer -n slg `
  -f ./deploy/helm/consumer/values-emulated.yaml
```

5) Verify the deployment in the cluster and expose the API:

```powershell
kubectl -n slg get deploy,pods,svc,cm
kubectl -n slg rollout status deploy/software-guide-api --timeout=180s
kubectl -n slg rollout status deploy/outbox-processor --timeout=120s
kubectl -n slg rollout status deploy/consumer --timeout=120s

kubectl -n slg port-forward svc/software-guide-api 5089:80   # in another terminal
```

Repeat the flow from Step 3 (token via `http://localhost:5089/api/v1/token` and `POST /api/v1/product`), and check worker logs:

```powershell
kubectl -n slg logs -l app.kubernetes.io/instance=outbox-processor -f --tail=50
kubectl -n slg logs -l app.kubernetes.io/instance=consumer -f --tail=50
```

7) Teardown of the emulated cluster:

```powershell
helm uninstall software-guide-api outbox-processor consumer -n slg
cd deploy/IaC
terraform destroy -var-file="terraform.minstack.tfvars.example" -auto-approve
```

> `terraform destroy` also deletes the k3s cluster embedded by MiniStack.

### Step 6 — Operate MiniStack with AWS CLI (SSM / SNS / SQS / Cognito)

With the `ministack` profile ready:

```powershell
# Get resolved configuration from SSM for each service
aws --profile ministack --endpoint-url http://localhost:4566 ssm get-parameters-by-path --path "/softwarelearningguide/" --recursive

# AWS messaging (MassTransit creates topics/queues on startup)
aws --profile ministack --endpoint-url http://localhost:4566 sns list-topics
aws --profile ministack --endpoint-url http://localhost:4566 sqs list-queues

# Cognito: pools/clients created by cognito-init.sh
aws --profile ministack --endpoint-url http://localhost:4566 cognito-idp list-user-pools --max-results 50
aws --profile ministack --endpoint-url http://localhost:4566 cognito-idp list-user-pool-clients --user-pool-id <POOL_ID>

# Get a token directly from MiniStack (alternative to /api/v1/token)
$CLIENT_ID = aws --profile ministack --endpoint-url http://localhost:4566 ssm get-parameter `
  --name "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/ClientId" `
  --query Parameter.Value --output text
aws --profile ministack --endpoint-url http://localhost:4566 cognito-idp initiate-auth `
  --client-id $CLIENT_ID --auth-flow USER_PASSWORD_AUTH `
  --auth-parameters USERNAME=admin@test.com,PASSWORD=Test1234!
```

### Step 7 — Teardown (Local Emulation)

```powershell
docker-compose down -v        # -v also removes volumes (SQL, Redis, RabbitMQ)
```

> Without `-v` data persists between restarts (MiniStack SSM/Cognito parameters don't survive: the container is recreated and seed scripts only run on first startup).

---

## Terraform: Without Credentials, With Credentials, or against MiniStack

The default state is **local** (`deploy/IaC/terraform.tfstate`) and the module **doesn't query AWS to plan** (credentials are bypassed by default). You can format, validate, and generate plans without creating anything and **without AWS credentials**:

```powershell
cd deploy/IaC
terraform init                 # downloads providers (only requires internet)
terraform fmt -recursive       # canonical HCL formatting
terraform validate             # validates syntax and references
terraform plan -var-file="terraform.tfvars.example"   # resource graph without contacting AWS
```

> The `.terraform.lock.hcl` generated by `terraform init` pins exact provider versions; **commit it to git** (`.terraform/` is in `.gitignore`). The `fmt` formats nested `.tf` files (modules) and `validate` checks variable and module references. All of this works without AWS credentials or a MiniStack connection.

The default flags in `variables.tf` (`aws_skip_credentials_validation=true`, `aws_skip_requesting_account_id=true`) prevent the provider from calling `STS` during `plan`/`apply`, so planning works without a credential chain. If you need **real AWS creation**, add valid credentials:

```powershell
aws configure
terraform apply -var-file="terraform.tfvars.example"   # creates VPC + EKS + ECR (has cost)
```

**The strong point of this IaC is that it also runs against MiniStack without touching AWS** (Section 3, Step 5 — the complete emulated flow). Use the dedicated var-file:

```powershell
terraform init
terraform plan  -var-file="terraform.minstack.tfvars.example"
terraform apply -var-file="terraform.minstack.tfvars.example"
```

> With `terraform.minstack.tfvars.example` all provider services point to `http://localhost:4566` and disable parts the emulator doesn't support (NAT, flow logs, node group, add-ons, Access Entries, OIDC). The emulated EKS cluster spins up a real k3s/k3d embedded in Docker.

### Quick Reference

| Scenario | Var-file | Credentials | Result |
|----------|----------|-------------|--------|
| Quick plan (no changes) | `terraform.tfvars.example` | None | Proposed graph; doesn't contact AWS |
| Real AWS deployment | `terraform.tfvars` | `aws configure` | Real VPC + EKS + ECR (real cost) |
| Emulated with MiniStack | `terraform.minstack.*` | `test`/`test` | VPC + ECR + k3s local on `localhost:4566` |

---

## Real Deployment in AWS (Reference)

> **Only run this when you want to deploy for real. Infrastructure = cost.** Here **kubectl + Helm** are used (not in the local flow).

### Create the Infrastructure

```powershell
cd deploy/IaC
copy terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars (admin_arns with YOUR ARNs, region, sizes...)

aws configure                    # real account credentials
terraform init
terraform plan -var-file="terraform.tfvars"
terraform apply -var-file="terraform.tfvars"   # ~10-15 min
```

Get useful values:

```powershell
terraform output kubeconfig_command
terraform output ecr_repository_urls
```

### kubeconfig and Access

```powershell
aws eks --region us-east-1 update-kubeconfig --name softwarelearningguide-dev-eks
kubectl get nodes
kubectl get ns
```

With `eks_admin_arns` filled, your user is already admin via Access Entries (`AmazonEKSClusterAdminPolicy`).

### Build and Publish Images to ECR

```powershell
$ACCOUNT = (aws sts get-caller-identity --query Account --output text)
$REGION  = "us-east-1"

docker build -f SoftwareLearningGuide.Api/Dockerfile                  -t softwarelearningguide/software-guide-api:local  .
docker build -f SoftwareLearningGuide.OutboxProcessor/Dockerfile      -t softwarelearningguide/outbox-processor:local    .
docker build -f SoftwareLearningGuide.Consumer/Dockerfile             -t softwarelearningguide/consumer:local            .

docker tag softwarelearningguide/software-guide-api:local   "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-api:latest"
docker tag softwarelearningguide/outbox-processor:local     "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-outboxprocessor:latest"
docker tag softwarelearningguide/consumer:local             "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-consumer:latest"

aws ecr get-login-password --region $REGION | docker login --username AWS `
  --password-stdin "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com"

docker push "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-api:latest"
docker push "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-outboxprocessor:latest"
docker push "$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-consumer:latest"
```

### Install Charts with ECR Images

> In real AWS the derived services (SQL, RabbitMQ, SNS/SQS, Cognito) are not in the cluster: create RDS SQL Server, AmazonMQ/RabbitMQ, SNS/SQS, and the Cognito user pool, save the configuration in SSM under `/softwarelearningguide/{env}/{service}/` and point the charts to those values (SSM wins over ConfigMap; with empty `ServiceUrl` the SDK uses real AWS). The charts' default values (`localhost`) are illustrative: adjust them with `--set`.

```powershell
helm upgrade --install software-guide-api ./deploy/helm/software-guide-api -n slg `
  --set image.repository="$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-api" `
  --set image.tag=latest

helm upgrade --install outbox-processor ./deploy/helm/outbox-processor -n slg `
  --set image.repository="$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-outboxprocessor" `
  --set image.tag=latest

helm upgrade --install consumer ./deploy/helm/consumer -n slg `
  --set image.repository="$ACCOUNT.dkr.ecr.$REGION.amazonaws.com/softwarelearningguide-consumer" `
  --set image.tag=latest
```

To expose the API outside the cluster, add an Ingress/ALB (or change `service.type` to `LoadBalancer` or `NodePort`).

---

## Daily Operations (AWS Real)

```powershell
# List installed releases
helm ls -n slg

# Update a chart (applies changes to values/config)
helm upgrade outbox-processor ./deploy/helm/outbox-processor -n slg

# See what would change without applying
helm template outbox-processor ./deploy/helm/outbox-processor -n slg

# Rollback
helm history software-guide-api -n slg
helm rollback software-guide-api 1 -n slg

# Scale / logs / applied config
kubectl -n slg scale deploy/software-guide-api --replicas=3
kubectl -n slg logs -l app.kubernetes.io/instance=consumer -f
kubectl -n slg get cm software-guide-api-config -o yaml | Select-Object -First 80
```

---

## Troubleshooting

| Symptom | Probable Cause | Solution |
|---------|----------------|----------|
| `aws ... endpoint` doesn't respond (Timeout) | MiniStack not running | `docker-compose ps` → `docker-compose up -d ministack redis` |
| SSM empty or partially seeded | Seed scripts didn't run (first upload with persistent volume) | `ministack.ps1 -Action Reseed`; or restart the container without volume; check logs: `docker compose logs ministack` |
| API returns `400` «Cognito not configured» | `UserPoolId`/`ClientId` not in SSM | Re-run `cognito-init.sh` inside the container (Section 5) and restart the API |
| `401` when calling the API | Token expired or not sent | Get one at `/api/v1/token` (Section 3, Step 3) |
| Endpoint returns `404` | Feature flag `FT_*` disabled in Flagsmith | Enable it in `http://localhost:8000` (in dev the API reads flags from local Flagsmith) |
| Outbox/Consumer no logs | Workers not started | Check the console windows: `dotnet run ... OutboxProcessor/Consumer` |
| Duplicate messages after restart | SQL/RabbitMQ retain state between sessions | `docker-compose down -v` for a full reset |
| `terraform plan` fails with `InvalidClientTokenId` | AWS credentials configured are not valid | `aws configure` to get real credentials, or use `terraform.minstack.tfvars.example` if going against MiniStack |
| Helm can't find local chart | Relative paths from another directory | Run from the repo root (`./deploy/helm/...`) |
| Pod `ImagePullBackOff` (AWS real) | Image doesn't exist in ECR or wrong tag | Publish it (Section 5.3) and verify `image.repository`/`image.tag` |
| `kubectl get nodes` → credentials error | Context uses `aws eks get-token`, which MiniStack's k3s doesn't validate | `ministack.ps1 -Action Kubeconfig` (uses the k3s container's admin kubeconfig) |

---

## Security and Best Practices

- **Isolated local emulation**: The `test`/`test` credentials and `ministack` profile only point to MiniStack; they are never used for real AWS calls.
- **Secrets outside the repo**: In the charts, values are provided as learning defaults. In production use `Secret` + SSM Parameter Store / Secrets Manager and inject them with `extraEnv` or `--set-string config...`.
- **ConfigMap immutable by checksum**: When `values.config` changes, the Deployment automatically re-deploys (`checksum/config` annotation).
- **Probes**: The API has `readiness`/`liveness` via TCP; the workers (no HTTP listener) **do not** have probes to prevent restart loops.
- **Bounded resources** (`requests`/`limits`) in all three charts.
- **ECR**: Scanning on push, lifecycle policy (retain N images, clean the rest).
- **EKS**: `authentication_mode = API_AND_CONFIG_MAP` with Access Entries (no manual `aws-auth` editing), control plane logging parameterized, public endpoint restrictable by CIDR, and OIDC provider ready for IRSA.
- **VPC**: Private subnets for workloads, NAT for egress, optional Flow Logs, and full tagging (`Name`, `Project`, `Environment`, `ManagedBy`).

---

## Related Documentation

| Topic | Document |
|-------|----------|
| **Terraform (IaC)** | [`README.Terraform.en.md`](README.Terraform.en.md) |
| **Helm (Kubernetes)** | [`README.Helm.en.md`](README.Helm.en.md) |
| **MiniStack (local emulator)** | [`README.Ministack.en.md`](README.Ministack.en.md) |
| **AWS general (service catalog)** | [`README.AWS.en.md`](README.AWS.en.md) |
| **API configuration** | [`SoftwareLearningGuide.Api/README.Api.en.md`](../SoftwareLearningGuide.Api/README.Api.en.md) |
| **Docker Compose local** | [`README.en.md`](../../../README.en.md) |
| **Feature Management** | [`SoftwareLearningGuide.Api/README.FeatureManagement.en.md`](../SoftwareLearningGuide.Api/README.FeatureManagement.en.md) |
| **Observability** | [`SoftwareLearningGuide.Api/README.Observability.en.md`](../SoftwareLearningGuide.Api/README.Observability.en.md) |

---

**Happy deployment!** 🚀
